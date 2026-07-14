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

    // 0..1 sky transmission of a single cell; 1 for unroofed, 0 for non-skylight roofs
    float CellLight(int x, int z)
    {
      IntVec3 c = new(x, 0, z);
      if (!c.InBounds(map)) return 0f;
      RoofDef roof = roofGrid.RoofAt(c);
      if (roof == null) return 1f;
      if (!Stats.RoofStatCache.IsSkylight(roof)) return 0f;
      return Stats.RoofStatCache.GetEffectiveTransparency(roof, coating, c);
    }

    for (int i = 0; i < colors.Length; i++)
    {
      int vx = Mathf.FloorToInt(verts[i].x);
      int vz = Mathf.FloorToInt(verts[i].z);
      // Corner verts sit on integer coords; cell-center verts sit at +0.5
      bool isCenter = verts[i].x != vx;

      float light;
      if (isCenter)
      {
        light = CellLight(vx, vz);
      }
      else
      {
        // Interior corners only: every in-bounds cell touching the vertex must be glass
        light = float.MaxValue;
        for (int dx = -1; dx <= 0; dx++)
        {
          for (int dz = -1; dz <= 0; dz++)
          {
            if (!new IntVec3(vx + dx, 0, vz + dz).InBounds(map)) continue;
            light = Mathf.Min(light, CellLight(vx + dx, vz + dz));
          }
        }
        if (light == float.MaxValue) light = 0f;
      }

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
