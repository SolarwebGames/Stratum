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
      var handlers = registry.GetHandlers<MapHookRegistry.PowerNetEnergyGainHandler>(MapHookRegistry.HookId.PowerNetEnergyGain);
      if (handlers != null)
      {
        for (int i = 0; i < handlers.Count; i++)
        {
          try
          {
            handlers[i](__instance, ref __result);
          }
          catch (System.Exception ex)
          {
            StratumLog.Error($"Error in CalculateEnergyGainRate subscriber: {ex}");
          }
        }
      }
    }
  }
}

