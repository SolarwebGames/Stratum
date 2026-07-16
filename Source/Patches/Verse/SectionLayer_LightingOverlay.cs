using HarmonyLib;
using UnityEngine;
using Verse;

namespace SolarWeb.Stratum.Patches;

[HarmonyPatch(typeof(SectionLayer_LightingOverlay), "GenerateLightingOverlay")]
public static class SectionLayer_LightingOverlay_Patch
{
  // The LightOverlay shader lerps between the material's sky color and the vertex glow color,
  // weighted by vertex alpha (0 = sky, 100 = glow/roofed). Skylights lower that alpha so the
  // sky shines through — and because the sky term comes from the material color, day/night
  // transitions animate for free. Nothing time-of-day-dependent may be baked into the mesh:
  // sections regenerate at unrelated times, and any baked sky brightness goes stale.
  //
  // The sky share must stay strictly inside the glass footprint. Corner verts are shared with
  // the surrounding cells' quads, so giving a boundary corner any sky share paints a ring on
  // the floor or walls around the skylight (dark whenever the sky is dimmer than the room,
  // bright otherwise). Only verts whose every touching cell is transparent glass may mix sky;
  // boundary glass cells feather from vanilla rim to bright center within themselves, like
  // vanilla courtyards.
  [HarmonyPostfix]
  public static void GenerateLightingOverlay_Postfix(
    Map map,
    LayerSubMesh subMesh,
    bool centered)
  {
    if (!Stratum.Settings.enableSkylightLighting) return;
    // Baked meshes (gravship capture) have verts offset from world space, so cell lookups would be wrong
    if (centered) return;
    if (map == null || map.roofGrid == null || subMesh?.mesh == null) return;

    var colors = subMesh.mesh.colors32;
    var verts = subMesh.mesh.vertices;
    if (colors == null || verts == null || colors.Length == 0 || verts.Length != colors.Length) return;

    RoofGrid roofGrid = map.roofGrid;
    var coating = map.GetComponent<MapComponents.SkylightCoating>();

    // 0..1 sky transmission of a single cell; 1 for unroofed, 0 for non-skylight roofs.
    // isGlass reports whether the cell is skylight glass specifically: vertices that touch
    // no glass at all must be left untouched. Vanilla already lights them, including the
    // half-cell shadow feather at roof edges (unroofed cell centers next to a roof average
    // to ~alpha 50) — overwriting those centers with a pure sky value while the shared edge
    // corners stay at 100 turns every roof line into a sawtooth of shadow spikes.
    float CellLight(int x, int z, out bool isGlass)
    {
      isGlass = false;
      IntVec3 c = new(x, 0, z);
      if (!c.InBounds(map)) return 0f;
      RoofDef roof = roofGrid.RoofAt(c);
      if (roof == null) return 1f;
      if (!Stats.RoofStatCache.IsSkylight(roof)) return 0f;
      isGlass = true;
      return Stats.RoofStatCache.GetEffectiveTransparency(roof, coating, c);
    }

    // Sky share of a shared corner vertex: the most opaque touching cell wins, so a corner
    // adjacent to any solid roof keeps vanilla's full-dark value (no ring around skylights),
    // while a corner between glass and open sky transmits like the glass.
    float CornerSky(int vx, int vz, out bool anyGlass)
    {
      float sky = float.MaxValue;
      anyGlass = false;
      for (int dx = -1; dx <= 0; dx++)
      {
        for (int dz = -1; dz <= 0; dz++)
        {
          if (!new IntVec3(vx + dx, 0, vz + dz).InBounds(map)) continue;
          sky = Mathf.Min(sky, CellLight(vx + dx, vz + dz, out bool glass));
          anyGlass |= glass;
        }
      }
      return sky == float.MaxValue ? 0f : sky;
    }

    for (int i = 0; i < colors.Length; i++)
    {
      int vx = Mathf.FloorToInt(verts[i].x);
      int vz = Mathf.FloorToInt(verts[i].z);
      // Corner verts sit on integer coords; cell-center verts sit at +0.5
      bool isCenter = verts[i].x != vx;

      float light;
      bool anyGlass;
      if (isCenter)
      {
        float cellLight = CellLight(vx, vz, out bool cellIsGlass);
        // Solid-roofed centers must stay vanilla (alpha 100)
        if (!cellIsGlass && roofGrid.RoofAt(new IntVec3(vx, 0, vz)) != null) continue;

        // Center verts must be the average of their corner sky values, mirroring vanilla's
        // corner-averaging: the triangle fan renders any center that deviates from that
        // average as a bright/dark diamond, so a glass center set straight to its own
        // transparency scallops the whole glass-solid boundary, and vanilla's own centers
        // (which counted glass corners as fully roofed) put a half-dark blob on every open
        // cell bordering a skylight. Along a straight glass edge the average is exactly
        // half the transparency, so the floor below only lifts narrow strips.
        anyGlass = cellIsGlass;
        float sum = 0f;
        sum += CornerSky(vx, vz, out bool g00); anyGlass |= g00;
        sum += CornerSky(vx + 1, vz, out bool g10); anyGlass |= g10;
        sum += CornerSky(vx, vz + 1, out bool g01); anyGlass |= g01;
        sum += CornerSky(vx + 1, vz + 1, out bool g11); anyGlass |= g11;
        light = sum / 4f;

        if (cellIsGlass)
        {
          // A skylight fully rimmed by solid roof averages to 0; keep it lit as a soft
          // pool rather than vanilla-dark (vanilla 1-wide courtyards get no sky at all).
          light = Mathf.Max(light, 0.5f * cellLight);
        }
      }
      else
      {
        light = CornerSky(vx, vz, out anyGlass);
      }

      if (!anyGlass) continue;
      if (light <= 0.001f) continue;

      // A vertex gets a sky share only to the extent transmission exceeds the glow already
      // there: mixing sky into a lamp-lit vertex darkens it whenever the sky is dimmer than
      // the lamps. Glow changes already dirty GroundGlow, so this input is never stale.
      Color32 col = colors[i];
      float glowStrength = Mathf.Max(col.r, Mathf.Max(col.g, col.b)) / 255f;
      float skyFrac = light - glowStrength;
      if (skyFrac <= 0f) continue;

      colors[i].a = (byte)Mathf.Clamp(100f * (1f - skyFrac), 0f, 100f);
    }

    subMesh.mesh.colors32 = colors;
  }
}
