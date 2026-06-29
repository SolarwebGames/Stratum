using HarmonyLib;
using SaveOurShip2;

namespace SolarWeb.Stratum.SOS2.Patches;

[HarmonyPatch(typeof(CompShipCachePart))]
public static class CompShipCachePart_Patch
{
  [HarmonyPatch(nameof(CompShipCachePart.PostSpawnSetup))]
  [HarmonyPrefix]
  public static void PostSpawnSetup_Prefix(CompShipCachePart __instance, bool respawningAfterLoad, out bool __state)
  {
    __state = false;
    if (respawningAfterLoad && __instance.Props != null)
    {
      __state = __instance.Props.roof;
      __instance.Props.roof = false;
    }
  }

  [HarmonyPatch(nameof(CompShipCachePart.PostSpawnSetup))]
  [HarmonyPostfix]
  public static void PostSpawnSetup_Postfix(CompShipCachePart __instance, bool respawningAfterLoad, bool __state)
  {
    if (respawningAfterLoad && __instance.Props != null)
    {
      __instance.Props.roof = __state;
    }
  }
}
