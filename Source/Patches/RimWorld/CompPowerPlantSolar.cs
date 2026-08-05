using HarmonyLib;
using RimWorld;
using Verse;

using SolarWeb.Stratum.DefModExtensions;
using SolarWeb.Stratum.Stats;
using SolarWeb.Stratum.Utilities;

namespace SolarWeb.Stratum.Patches;

[HarmonyPatch]
public static class CompPowerPlantSolar_Patch
{
  [HarmonyPatch(typeof(CompPowerPlantSolar), "RoofedPowerOutputFactor", MethodType.Getter)]
  [HarmonyPrefix]
  public static bool RoofedPowerOutputFactor_Prefix(CompPowerPlantSolar __instance, ref float __result)
  {
    if (__instance.parent == null) return true;
    var map = __instance.parent.Map;
    if (map == null) return true;

    bool isRooftop = RoofBuildings.IsRoofBuildingOrBlueprintOrFrame(__instance.parent) &&
                     RoofBuildings.GetAttachmentType(__instance.parent) == RoofAttachmentType.Rooftop;

    int totalCells = 0;
    float totalPassage = 0f;

    foreach (IntVec3 item in __instance.parent.OccupiedRect())
    {
      totalCells++;
      float cellFactor;
      if (isRooftop)
      {
        cellFactor = 1f;
      }
      else
      {
        var roof = map.roofGrid.RoofAt(item);
        cellFactor = roof == null ? 1f : RoofStatCache.GetEffectiveTransparency(roof, map, item);
      }
      cellFactor = Hooks.MapHookRegistry.GetCellSolarPowerOutputFactor(map, item, cellFactor);
      totalPassage += cellFactor;
    }

    __result = totalCells > 0 ? totalPassage / totalCells : 0f;
    return false;
  }
}

