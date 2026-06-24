using HarmonyLib;
using RimWorld;
using Verse;

namespace SolarWeb.Stratum.Patches;

[HarmonyPatch]
public static class Area_Patch
{
  [HarmonyPatch(typeof(Area), "Set")]
  [HarmonyPostfix]
  public static void Set_Postfix(Area __instance, IntVec3 c, ref bool val)
  {
    if (__instance.Map != null && __instance == __instance.Map.areaManager.NoRoof)
    {
      __instance.Map.mapDrawer.MapMeshDirty(c, MapMeshFlagDefOf.Roofs);
    }
  }
}
