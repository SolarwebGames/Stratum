using HarmonyLib;
using RimWorld;
using Verse;

namespace SolarWeb.Stratum.Patches;

[HarmonyPatch]
public static class WorldComponent_GravshipController_Patch
{
  [HarmonyPatch(typeof(WorldComponent_GravshipController), "RegenerateGravshipMask")]
  [HarmonyPostfix]
  public static void RegenerateGravshipMask_Postfix(WorldComponent_GravshipController __instance)
  {
    Map? map = Traverse.Create(__instance).Field("map").GetValue<Map>();
    map?.mapDrawer.WholeMapChanged((ulong)MapMeshFlagDefOf.Roofs);
  }
}