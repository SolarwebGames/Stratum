using HarmonyLib;
using UnityEngine;
using Verse;

namespace SolarWeb.Stratum.Patches;

[HarmonyPatch(typeof(SectionLayer_LightingOverlay), "GenerateLightingOverlay")]
public static class SectionLayer_LightingOverlay_Patch
{
  [HarmonyPostfix]
  public static void GenerateLightingOverlay_Postfix(
    Map map,
    LayerSubMesh subMesh)
  {
    if (!SolarWeb.Stratum.Stratum.Settings.enableSkylightShadows) return;
    if (map == null || map.roofGrid == null || subMesh?.mesh == null) return;

    var colors = subMesh.mesh.colors32;
    var verts = subMesh.mesh.vertices;
    if (colors == null || verts == null || colors.Length == 0 || verts.Length != colors.Length) return;

    RoofGrid roofGrid = map.roofGrid;

    for (int i = 0; i < colors.Length; i++)
    {
      int vx = Mathf.FloorToInt(verts[i].x);
      int vz = Mathf.FloorToInt(verts[i].z);

      float maxTrans = 0f;
      bool hasSkylight = false;

      for (int dx = -1; dx <= 0; dx++)
      {
        for (int dz = -1; dz <= 0; dz++)
        {
          IntVec3 c = new(vx + dx, 0, vz + dz);
          if (c.InBounds(map))
          {
            RoofDef roof = roofGrid.RoofAt(c);
            if (roof != null && SolarWeb.Stratum.Stats.RoofStatCache.IsSkylight(roof))
            {
              hasSkylight = true;
              float t = SolarWeb.Stratum.Stats.RoofStatCache.GetEffectiveTransparency(roof, map, c);
              if (t > maxTrans) maxTrans = t;
            }
          }
        }
      }

      if (hasSkylight)
      {
        byte baseA = (byte)Mathf.Clamp(100f * (1f - maxTrans), 0f, 100f);
        colors[i].a = SolarWeb.Stratum.Graphics.SectionLayer_RoofLightingAndShadows.GetSkylightCornerShadowAlpha(map, roofGrid, vx, vz, baseA);
      }
    }

    subMesh.mesh.colors32 = colors;
  }
}
