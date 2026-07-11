using HarmonyLib;
using RimWorld;
using Verse;

using SolarWeb.Stratum.Stats;
using SolarWeb.Stratum.MapComponents;

namespace SolarWeb.Stratum.Patches;

[HarmonyPatch]
public static class CompPowerPlantSolar_Patch
{
  [HarmonyPatch(typeof(CompPowerPlantSolar), "RoofedPowerOutputFactor", MethodType.Getter)]
  [HarmonyPrefix]
  public static bool RoofedPowerOutputFactor_Prefix(CompPowerPlantSolar __instance, ref float __result)
  {
    int totalCells = 0;
    float totalPassage = 0f;
    var map = __instance.parent.Map;

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
        float transparency = RoofStatCache.GetTransparency(roof);
        var coating = map.GetComponent<SkylightCoating>();
        if (coating != null)
        {
          transparency *= (1f - coating.GetCoatingOpacity(item));
        }
        totalPassage += transparency;
      }
    }

    __result = totalPassage / totalCells;
    return false;
  }
}
