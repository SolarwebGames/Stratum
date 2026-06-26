using HarmonyLib;
using RimWorld;
using SolarWeb.Stratum.Hooks;

namespace SolarWeb.Stratum.Patches;

[HarmonyPatch]
public static class PowerNet_Patch
{
  [HarmonyPatch(typeof(PowerNet), nameof(PowerNet.CurrentEnergyGainRate))]
  [HarmonyPostfix]
  public static void CurrentEnergyGainRate_Postfix(PowerNet __instance, ref float __result)
  {
    var registry = MapHookRegistry.Get(__instance.Map);
    if (registry != null)
    {
      registry.Notify_CalculateEnergyGainRate(__instance, ref __result);
    }
  }
}

