using HarmonyLib;
using SaveOurShip2;

using SolarWeb.Stratum.Stats;

namespace SolarWeb.Stratum.SOS2.Patches;

[HarmonyPatch(typeof(CompPowerPlantSolarShip))]
public static class CompPowerPlantSolarShip_Patch
{
  [HarmonyPatch("RoofedPowerOutputFactor", MethodType.Getter)]
  [HarmonyPrefix]
  public static bool RoofedPowerOutputFactor_Prefix(CompPowerPlantSolarShip __instance, ref float __result)
  {
    if (__instance.unfoldTo == null) return true;

    int total = 0;
    float passage = 0f;
    var map = __instance.parent.Map;

    foreach (var c in __instance.unfoldTo)
    {
      total++;
      var roof = map.roofGrid.RoofAt(c);
      if (roof == null)
      {
        passage += 1f;
      }
      else
      {
        if (RoofStatCache.IsCustomRoof(roof))
        {
          passage += RoofStatCache.GetTransparency(roof);
        }
        else
        {
          if (!map.roofGrid.Roofed(c))
          {
            passage += 1f;
          }
        }
      }
    }

    __result = total > 0 ? passage / total : 1f;
    return false;
  }
}
