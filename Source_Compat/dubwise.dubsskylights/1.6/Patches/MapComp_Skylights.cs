using HarmonyLib;
using Dubs_Skylight;
using Verse;

using SolarWeb.Stratum.Stats;

namespace SolarWeb.Stratum.DubsSkylights.Patches;

[HarmonyPatch(typeof(MapComp_Skylights))]
public static class MapComp_Skylights_Patch
{
  [HarmonyPatch(nameof(MapComp_Skylights.RegenGrid))]
  [HarmonyPostfix]
  public static void RegenGrid_Postfix(MapComp_Skylights __instance)
  {
    Map map = __instance.map;
    if (map == null || map.roofGrid == null) return;

    for (int i = 0; i < map.cellIndices.NumGridCells; i++)
    {
      var roof = map.roofGrid.RoofAt(i);
      bool isSkylightRoof = roof != null && RoofStatCache.IsSkylight(roof);

      if (__instance.SkylightGrid[i] && !isSkylightRoof)
      {
        __instance.SkylightGrid[i] = false;
      }
      else if (isSkylightRoof)
      {
        __instance.SkylightGrid[i] = true;
      }
    }
  }
}
