using HarmonyLib;
using RimWorld;

namespace SolarWeb.Stratum.Patches;

[HarmonyPatch]
public static class PowerNet_Patch
{
  [HarmonyPatch(typeof(PowerNet), nameof(PowerNet.CurrentEnergyGainRate))]
  [HarmonyPostfix]
  public static void CurrentEnergyGainRate_Postfix(PowerNet __instance, ref float __result)
  {
    Utilities.StratumHooks.OnCalculateEnergyGainRate?.Invoke(__instance, ref __result);
  }
}
