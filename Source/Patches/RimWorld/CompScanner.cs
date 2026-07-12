using HarmonyLib;
using RimWorld;
using UnityEngine;

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
      float baseSpeed = addedProgress * 60000f;
      float effectiveSpeed = ScannerBoosterUtility.GetScanSpeed(__instance.parent, baseSpeed);
      if (!Mathf.Approximately(effectiveSpeed, baseSpeed))
      {
        daysWorkingSinceLastFindingRef(__instance) = __state + (effectiveSpeed / 60000f);
      }
    }
  }

  [HarmonyPatch("TickDoesFind")]
  [HarmonyPrefix]
  public static void TickDoesFind_Prefix(CompScanner __instance, ref float scanSpeed)
  {
    scanSpeed = ScannerBoosterUtility.GetScanSpeed(__instance.parent, scanSpeed);
  }
}
