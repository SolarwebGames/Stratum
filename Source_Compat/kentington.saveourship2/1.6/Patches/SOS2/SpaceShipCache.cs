using System;
using HarmonyLib;
using SaveOurShip2;
using Verse;
using SolarWeb.Stratum.SOS2.WorldComponents;

namespace SolarWeb.Stratum.SOS2.Patches;

[HarmonyPatch(typeof(SpaceShipCache))]
public static class SpaceShipCache_Patch
{
  [HarmonyPatch(nameof(SpaceShipCache.CreateShipSketch))]
  [HarmonyPrefix]
  static void CreateShipSketch_Prefix(SpaceShipCache __instance)
  {
    try
    {
      if (__instance.Area != null && __instance.Map != null && __instance.Index >= 0)
      {
        var origin = __instance.Core?.Position ?? IntVec3.Zero;
        SOS2RoofTracker.CaptureShipRoofs(__instance.Map, __instance.Area, __instance.Index, origin);
      }
    }
    catch (Exception e)
    {
      StratumLog.Warning($"Error capturing SOS2 ship roofs: {e.Message}");
    }
  }
}

