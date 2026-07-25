using RimWorld;
using UnityEngine;
using Verse;

using SolarWeb.Stratum.Stats;

namespace SolarWeb.Stratum.Graphics;

[StaticConstructorOnStartup]
public class RoofLightingRenderer : SectionLayer
{
  private static Material? skylightTintMat;
  private readonly Color32[] vertTintCache = new Color32[18 * 18];

  private static Material SkylightTintMat
  {
    get
    {
      if (skylightTintMat == null)
      {
        skylightTintMat = new Material(MatBases.LightOverlay.shader)
        {
          mainTexture = BaseContent.WhiteTex,
          renderQueue = 3162
        };
      }
      return skylightTintMat;
    }
  }

  public RoofLightingRenderer(Section section) : base(section)
  {
    relevantChangeTypes = MapMeshFlagDefOf.Roofs | MapMeshFlagDefOf.Buildings;
  }

  public override bool Visible => true;

  public override void DrawLayer()
  {
    if (skylightTintMat != null)
    {
      skylightTintMat.color = MatBases.LightOverlay.color;
    }
    base.DrawLayer();
  }

  public override void Regenerate()
  {
    ClearSubMeshes(MeshParts.All);

    Map map = section.map;
    if (map == null || map.roofGrid == null) return;

    RoofGrid roofGrid = map.roofGrid;
    int mapSizeX = map.Size.x;
    int mapSizeZ = map.Size.z;
    var coating = map.GetComponent<MapComponents.SkylightCoating>();
    var integrity = map.GetComponent<MapComponents.RoofIntegrityGrid>();
    LayerSubMesh tintMesh = GetSubMesh(SkylightTintMat);
    float y = AltitudeLayer.LightingOverlay.AltitudeFor() + 0.002f;
    CellRect rect = section.CellRect;

    int vertWidth = rect.Width + 1;
    int vertHeight = rect.Height + 1;
    int reqLen = vertWidth * vertHeight;

    System.Array.Clear(vertTintCache, 0, reqLen);

    Color32 GetCachedVertexTint(int vx, int vz)
    {
      int index = (vz - rect.minZ) * vertWidth + (vx - rect.minX);
      if (vertTintCache[index].a == 255) return vertTintCache[index];

      Color32 color = GetTintVertexColorMultiplier(map, roofGrid, integrity, coating, vx, vz, mapSizeX, mapSizeZ);
      vertTintCache[index] = color;
      return color;
    }

    for (int z = rect.minZ; z <= rect.maxZ; z++)
    {
      for (int x = rect.minX; x <= rect.maxX; x++)
      {
        RoofDef roof = roofGrid.RoofAt(new IntVec3(x, 0, z));
        if (roof == null || !RoofStatCache.IsSkylight(roof)) continue;

        Color32 cBL = GetCachedVertexTint(x, z);
        Color32 cBR = GetCachedVertexTint(x + 1, z);
        Color32 cTL = GetCachedVertexTint(x, z + 1);
        Color32 cTR = GetCachedVertexTint(x + 1, z + 1);

        if (HasTint(cBL) || HasTint(cBR) || HasTint(cTL) || HasTint(cTR))
        {
          AppendQuadColored(tintMesh, x, z, x + 1f, z + 1f, y, cBL, cTL, cTR, cBR);
        }
      }
    }

    if (tintMesh.verts.Count > 0)
    {
      tintMesh.finalized = false;
      tintMesh.FinalizeMesh(MeshParts.All);
    }
  }

  private static bool HasTint(Color32 c) => c.r < 255 || c.g < 255 || c.b < 255;

  private static Color32 GetTintVertexColorMultiplier(Map map, RoofGrid roofGrid, MapComponents.RoofIntegrityGrid? integrity, MapComponents.SkylightCoating? coating, int vx, int vz, int mapSizeX, int mapSizeZ)
  {
    // This mesh is a second multiplicative lighting pass, so "no tint" must be white with
    // alpha 255 (alpha 255 = fully roof-covered, which makes the shader use the vertex
    // color alone). Anything darker double-darkens the scene under and around the skylight.
    float sumR = 0f, sumG = 0f, sumB = 0f;
    int count = 0;

    for (int dx = -1; dx <= 0; dx++)
    {
      for (int dz = -1; dz <= 0; dz++)
      {
        int cx = vx + dx;
        int cz = vz + dz;
        if ((uint)cx >= (uint)mapSizeX || (uint)cz >= (uint)mapSizeZ) continue;

        count++;
        float strength = 0f;
        Color tint = Color.white;

        IntVec3 c = new(cx, 0, cz);
        RoofDef roof = roofGrid.RoofAt(c);
        if (roof != null && RoofStatCache.IsSkylight(roof))
        {
          tint = RoofStatCache.GetGlassTint(roof, integrity, c);
          if (tint != Color.white)
          {
            strength = RoofStatCache.GetEffectiveTransparency(roof, coating, c) * 0.70f;
          }
        }

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

  private static void AppendQuadColored(LayerSubMesh sm, float minX, float minZ, float maxX, float maxZ, float y,
                                        Color32 cBL, Color32 cTL, Color32 cTR, Color32 cBR)
  {
    int i = sm.verts.Count;
    sm.verts.Add(new Vector3(minX, y, minZ)); // botLeft
    sm.verts.Add(new Vector3(minX, y, maxZ)); // topLeft
    sm.verts.Add(new Vector3(maxX, y, maxZ)); // topRight
    sm.verts.Add(new Vector3(maxX, y, minZ)); // botRight

    sm.colors.Add(cBL);
    sm.colors.Add(cTL);
    sm.colors.Add(cTR);
    sm.colors.Add(cBR);

    sm.tris.Add(i);
    sm.tris.Add(i + 1);
    sm.tris.Add(i + 2);
    sm.tris.Add(i);
    sm.tris.Add(i + 2);
    sm.tris.Add(i + 3);
  }
}
