using HarmonyLib;
using RimWorld;
using Verse;

using SolarWeb.Stratum.Utilities;

namespace SolarWeb.Stratum.Patches.RimWorld;

[HarmonyPatch(typeof(CompScanner))]
public static class CompScanner_Patch
{
  private static readonly AccessTools.FieldRef<CompScanner, float> daysWorkingSinceLastFindingRef =
    AccessTools.FieldRefAccess<CompScanner, float>("daysWorkingSinceLastFinding");

  [HarmonyPatch(nameof(CompScanner.Used))]
  [HarmonyPrefix]
  public static void Used_Prefix(CompScanner __instance, ref float __state)
  {
    __state = daysWorkingSinceLastFindingRef(__instance);
  }

  [HarmonyPatch(nameof(CompScanner.Used))]
  [HarmonyPostfix]
  public static void Used_Postfix(CompScanner __instance, float __state)
  {
    float currentProgress = daysWorkingSinceLastFindingRef(__instance);
    float addedProgress = currentProgress - __state;
    if (addedProgress > 0f)
    {
      float multiplier = DefModExtensions.ScannerBooster.GetBoosterMultiplier(__instance.parent.Map, __instance.parent.Position);
      if (multiplier > 1f)
      {
        daysWorkingSinceLastFindingRef(__instance) = __state + (addedProgress * multiplier);
      }
    }
  }

  [HarmonyPatch("TickDoesFind")]
  [HarmonyPrefix]
  public static void TickDoesFind_Prefix(CompScanner __instance, ref float scanSpeed)
  {
    float multiplier = DefModExtensions.ScannerBooster.GetBoosterMultiplier(__instance.parent.Map, __instance.parent.Position);
    scanSpeed *= multiplier;
  }
}
