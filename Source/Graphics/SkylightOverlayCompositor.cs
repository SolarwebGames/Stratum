using UnityEngine;
using Verse;

using SolarWeb.Stratum.Hooks;
using SolarWeb.Stratum.Stats;

namespace SolarWeb.Stratum.Graphics;

// Shared per-section skylight state for the two systems that need it: the lighting overlay
// postfix (SectionLayer_LightingOverlay_Patch) and the glass tint pass (RoofLightingRenderer).
// Both run inside MapDrawer section regeneration on the main thread and fully consume the grid
// within one call, so the scratch buffers below can be static. Do not call this off-thread.
public static class SkylightOverlayCompositor
{
  private static float[] cellLight = [];
  private static bool[] cellIsGlass = [];
  private static bool[] cellIsSkylight = [];
  private static bool[] cellInBounds = [];
  private static bool[] cellRoofed = [];
  private static Color[] cellTint = [];
  private static float[] cornerSky = [];
  private static bool[] cornerHasGlass = [];

  private static int gridWidth;
  private static int gridHeight;
  private static int cornerWidth;
  private static int cornerHeight;
  private static int sectionMinX;
  private static int sectionMinZ;
  private static bool anyGlass;
  private static bool anySkylight;

  public static bool ApplyToLightingOverlay(Map map, CellRect rect, LayerSubMesh subMesh)
  {
    BuildCellGrid(map, rect, includeTint: false);
    if (!anyGlass) return false;
    BuildCornerGrid();
    return WriteVertexAlphas(subMesh);
  }

  public static bool TryBuildTintSection(Map map, CellRect rect)
  {
    BuildCellGrid(map, rect, includeTint: true);
    return anySkylight;
  }

  public static bool IsSkylightCell(int x, int z) => cellIsSkylight[CellIndex(x, z)];

  private static int CellIndex(int x, int z) => (z - sectionMinZ + 1) * gridWidth + (x - sectionMinX + 1);

  private static int CornerIndex(int vx, int vz) => (vz - sectionMinZ) * cornerWidth + (vx - sectionMinX);

  private static void EnsureCapacity(int cellCount, int cornerCount, bool includeTint)
  {
    if (cellLight.Length < cellCount)
    {
      cellLight = new float[cellCount];
      cellIsGlass = new bool[cellCount];
      cellIsSkylight = new bool[cellCount];
      cellInBounds = new bool[cellCount];
      cellRoofed = new bool[cellCount];
    }
    if (includeTint && cellTint.Length < cellCount)
    {
      cellTint = new Color[cellCount];
    }
    if (cornerSky.Length < cornerCount)
    {
      cornerSky = new float[cornerCount];
      cornerHasGlass = new bool[cornerCount];
    }
  }

  // Covers the section expanded by one cell on every side, because a corner vertex on the
  // section boundary samples the cells just outside it. Sets anyGlass and anySkylight so both
  // consumers can bail before touching any mesh.
  private static void BuildCellGrid(Map map, CellRect rect, bool includeTint)
  {
    sectionMinX = rect.minX;
    sectionMinZ = rect.minZ;
    gridWidth = rect.Width + 2;
    gridHeight = rect.Height + 2;
    cornerWidth = rect.Width + 1;
    cornerHeight = rect.Height + 1;
    EnsureCapacity(gridWidth * gridHeight, cornerWidth * cornerHeight, includeTint);

    RoofGrid roofGrid = map.roofGrid;
    var coating = map.GetComponent<MapComponents.SkylightCoating>();
    var integrity = includeTint ? map.GetComponent<MapComponents.RoofIntegrityGrid>() : null;
    bool applyTransmissionHook = MapHookRegistry.HasSkylightTransmissionHandlers(map);

    anyGlass = false;
    anySkylight = false;
    for (int gz = 0; gz < gridHeight; gz++)
    {
      int rowBase = gz * gridWidth;
      int z = sectionMinZ - 1 + gz;
      for (int gx = 0; gx < gridWidth; gx++)
      {
        int i = rowBase + gx;
        IntVec3 cell = new(sectionMinX - 1 + gx, 0, z);

        cellLight[i] = 0f;
        cellIsGlass[i] = false;
        cellIsSkylight[i] = false;
        cellRoofed[i] = false;
        if (includeTint) cellTint[i] = Color.white;

        cellInBounds[i] = cell.InBounds(map);
        if (!cellInBounds[i]) continue;

        RoofDef roof = roofGrid.RoofAt(cell);
        cellRoofed[i] = roof != null;
        cellIsSkylight[i] = roof != null && RoofStatCache.IsSkylight(roof);
        if (cellIsSkylight[i]) anySkylight = true;

        float transmission = roof == null ? 1f
          : cellIsSkylight[i] ? RoofStatCache.GetEffectiveTransparency(roof, coating, cell)
          : 0f;

        if (applyTransmissionHook)
        {
          transmission = MapHookRegistry.GetCellSkylightTransmission(map, cell, transmission);
        }

        cellLight[i] = transmission;

        // Only a roofed cell may count as glass. An open cell already transmits fully, and
        // flagging it here would let the overlay overwrite vanilla's half-cell shadow feather
        // at roof edges, turning every roof line into a sawtooth of shadow spikes.
        if (roof == null || transmission <= 0f) continue;

        cellIsGlass[i] = true;
        anyGlass = true;
        if (includeTint) cellTint[i] = RoofStatCache.GetGlassTint(roof, integrity, cell);
      }
    }
  }

  // The most opaque touching cell wins, so a corner adjacent to any solid roof keeps vanilla's
  // full-dark value and no ring appears on the floor or walls around a skylight, while a corner
  // between glass and open sky transmits like the glass.
  private static void BuildCornerGrid()
  {
    for (int vz = 0; vz < cornerHeight; vz++)
    {
      for (int vx = 0; vx < cornerWidth; vx++)
      {
        float sky = float.MaxValue;
        bool hasGlass = false;
        for (int dz = 0; dz <= 1; dz++)
        {
          int rowBase = (vz + dz) * gridWidth;
          for (int dx = 0; dx <= 1; dx++)
          {
            int i = rowBase + vx + dx;
            if (!cellInBounds[i]) continue;
            if (cellLight[i] < sky) sky = cellLight[i];
            hasGlass |= cellIsGlass[i];
          }
        }
        int c = vz * cornerWidth + vx;
        cornerSky[c] = sky == float.MaxValue ? 0f : sky;
        cornerHasGlass[c] = hasGlass;
      }
    }
  }

  // Vanilla's vertex layout is derived from the rect alone (MakeBaseGeometry emits
  // (W+1)*(H+1) corner verts row-major, then W*H center verts), so cell coordinates are index
  // arithmetic and mesh.vertices never has to be read back.
  private static bool WriteVertexAlphas(LayerSubMesh subMesh)
  {
    var colors = subMesh.mesh.colors32;
    int cornerCount = cornerWidth * cornerHeight;
    int centerCount = (cornerWidth - 1) * (cornerHeight - 1);
    if (colors == null || colors.Length != cornerCount + centerCount) return false;

    bool dirty = false;
    for (int i = 0; i < colors.Length; i++)
    {
      float sky;
      bool hasGlass;

      if (i < cornerCount)
      {
        sky = cornerSky[i];
        hasGlass = cornerHasGlass[i];
      }
      else if (!TryGetCenterVertexSky(i - cornerCount, out sky, out hasGlass))
      {
        continue;
      }

      if (!hasGlass || sky <= 0.001f) continue;
      if (TryMixSkyIntoVertex(ref colors[i], sky)) dirty = true;
    }

    if (dirty) subMesh.mesh.colors32 = colors;
    return dirty;
  }

  private static bool TryGetCenterVertexSky(int centerIndex, out float sky, out bool hasGlass)
  {
    int width = cornerWidth - 1;
    int lx = centerIndex % width;
    int lz = centerIndex / width;
    int cell = (lz + 1) * gridWidth + (lx + 1);

    sky = 0f;
    hasGlass = cellIsGlass[cell];

    // Solid-roofed centers must stay vanilla (alpha 100)
    if (!hasGlass && cellRoofed[cell]) return false;

    // A center vertex must be the average of its four corners, mirroring vanilla's own
    // corner-averaging. The triangle fan renders any center that deviates from that average as a
    // bright or dark diamond, so setting a glass center straight to its own transparency scallops
    // the whole glass-solid boundary.
    int c00 = lz * cornerWidth + lx;
    int c10 = c00 + 1;
    int c01 = c00 + cornerWidth;
    int c11 = c01 + 1;

    sky = (cornerSky[c00] + cornerSky[c10] + cornerSky[c01] + cornerSky[c11]) / 4f;
    hasGlass |= cornerHasGlass[c00] || cornerHasGlass[c10] || cornerHasGlass[c01] || cornerHasGlass[c11];

    // A skylight fully rimmed by solid roof averages to zero; keep it lit as a soft pool.
    if (cellIsGlass[cell]) sky = Mathf.Max(sky, 0.5f * cellLight[cell]);
    return true;
  }

  // Vertex alpha selects between the material's sky colour (0) and the vertex glow colour (100),
  // so the sky term animates with time of day for free and nothing time-dependent may be baked
  // in. A vertex earns a sky share only to the extent transmission exceeds the glow already
  // there, otherwise mixing sky into a lamp-lit vertex darkens it whenever the sky is dimmer.
  private static bool TryMixSkyIntoVertex(ref Color32 color, float sky)
  {
    float glowStrength = Mathf.Max(color.r, Mathf.Max(color.g, color.b)) / 255f;
    float skyFraction = sky - glowStrength;
    if (skyFraction <= 0f) return false;

    byte alpha = (byte)Mathf.Clamp(100f * (1f - skyFraction), 0f, 100f);
    if (alpha == color.a) return false;

    color.a = alpha;
    return true;
  }

  // The tint mesh is a second multiplicative pass, so "no tint" must be white at alpha 255
  // (alpha 255 = fully roof-covered, which makes the shader use the vertex colour alone).
  // Anything darker double-darkens the scene under and around the skylight.
  public static Color32 GetCornerTint(int vx, int vz)
  {
    float sumR = 0f, sumG = 0f, sumB = 0f;
    int count = 0;

    for (int dx = -1; dx <= 0; dx++)
    {
      for (int dz = -1; dz <= 0; dz++)
      {
        int i = CellIndex(vx + dx, vz + dz);
        if (!cellInBounds[i]) continue;

        count++;
        Color tint = cellTint[i];
        float strength = tint == Color.white ? 0f : cellLight[i] * 0.70f;

        sumR += Mathf.Lerp(1f, tint.r, strength) * 255f;
        sumG += Mathf.Lerp(1f, tint.g, strength) * 255f;
        sumB += Mathf.Lerp(1f, tint.b, strength) * 255f;
      }
    }

    if (count == 0) return new Color32(255, 255, 255, 255);

    return new Color32(
      (byte)Mathf.Clamp(sumR / count, 0f, 255f),
      (byte)Mathf.Clamp(sumG / count, 0f, 255f),
      (byte)Mathf.Clamp(sumB / count, 0f, 255f),
      255
    );
  }
}
