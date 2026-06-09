using HarmonyLib;
using RimWorld;
using SolarWeb.Stratum.MapComponents;

namespace SolarWeb.Stratum.Patches;

[HarmonyPatch]
public static class PowerNet_Patch
{
  [HarmonyPatch(typeof(PowerNet), nameof(PowerNet.CurrentEnergyGainRate))]
  [HarmonyPostfix]
  public static void CurrentEnergyGainRate_Postfix(PowerNet __instance, ref float __result)
  {
    var map = __instance.Map;
    if (map == null) return;

    var comp = map.GetComponent<SolarRoofMapComponent>();
    if (comp != null)
    {
      float solarPower = comp.GetAdditionalPowerFor(__instance);
      // Convert Watts to Energy per tick
      __result += solarPower / 60000f;
    }
  }
}
