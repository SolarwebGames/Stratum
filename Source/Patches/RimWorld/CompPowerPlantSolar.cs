using HarmonyLib;
using RimWorld;
using Verse;

using SolarWeb.Stratum.DefModExtensions;
using SolarWeb.Stratum.Stats;
using SolarWeb.Stratum.Utilities;
using SolarWeb.Stratum.MapComponents;

namespace SolarWeb.Stratum.Patches;

[HarmonyPatch]
public static class CompPowerPlantSolar_Patch
{
  [HarmonyPatch(typeof(CompPowerPlantSolar), "RoofedPowerOutputFactor", MethodType.Getter)]
  [HarmonyPrefix]
  public static bool RoofedPowerOutputFactor_Prefix(CompPowerPlantSolar __instance, ref float __result)
  {
    if (__instance.parent != null && RoofBuildings.IsRoofBuildingOrBlueprintOrFrame(__instance.parent))
    {
      var attachmentType = RoofBuildings.GetAttachmentType(__instance.parent);
      if (attachmentType == RoofAttachmentType.Rooftop)
      {
        __result = 1f;
        return false;
      }
    }

    if (__instance.parent == null) return true;
    var map = __instance.parent.Map;
    if (map == null) return true;

    int totalCells = 0;
    float totalPassage = 0f;

    foreach (IntVec3 item in __instance.parent.OccupiedRect())
    {
      totalCells++;
      var roof = map.roofGrid.RoofAt(item);
      if (roof == null)
      {
        totalPassage += 1f;
      }
      else
      {
        float transparency = RoofStatCache.GetEffectiveTransparency(roof, map, item);
        totalPassage += transparency;
      }
    }

    __result = totalPassage / totalCells;
    return false;
  }
}

