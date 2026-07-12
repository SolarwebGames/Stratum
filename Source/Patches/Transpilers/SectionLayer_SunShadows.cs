using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;
using Verse;

namespace SolarWeb.Stratum.Patches.Transpilers;

[HarmonyPatch]
public static class SectionLayer_SunShadows_Regenerate_Patch
{
  [HarmonyPatch("Verse.SectionLayer_SunShadows", "Regenerate")]
  [HarmonyPostfix]
  public static void Regenerate_Postfix(SectionLayer __instance, Section ___section)
  {
    if (___section == null || ___section.map == null) return;
    Map map = ___section.map;
    RoofGrid roofGrid = map.roofGrid;
    if (roofGrid == null) return;

    // Suppress all vanilla sun shadows inside roofed cells to prevent weird skewed shadow artifacts under roofs
    int subMeshCount = __instance.subMeshes.Count;
    for (int m = 0; m < subMeshCount; m++)
    {
      LayerSubMesh subMesh = __instance.subMeshes[m];
      if (subMesh.verts == null || subMesh.colors == null) continue;

      for (int i = 0; i < subMesh.verts.Count && i < subMesh.colors.Count; i++)
      {
        IntVec3 c = subMesh.verts[i].ToIntVec3();
        if (c.InBounds(map) && roofGrid.Roofed(c))
        {
          RoofDef roof = roofGrid.RoofAt(c);
          if (roof != null && SolarWeb.Stratum.Stats.RoofStatCache.IsSkylight(roof) &&
              SolarWeb.Stratum.Stats.RoofStatCache.GetEffectiveTransparency(roof, map, c) > 0f)
          {
            subMesh.colors[i] = new Color32(0, 0, 0, 0);
          }
        }
      }
    }
  }
}
