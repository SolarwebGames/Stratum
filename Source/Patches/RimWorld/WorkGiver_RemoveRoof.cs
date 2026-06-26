using HarmonyLib;
using Verse;
using RimWorld;

using SolarWeb.Stratum.Utilities;

namespace SolarWeb.Stratum.Patches;

[HarmonyPatch(typeof(WorkGiver_RemoveRoof))]
public static class WorkGiver_RemoveRoof_Patch
{
  [HarmonyPatch(nameof(WorkGiver_RemoveRoof.HasJobOnCell))]
  [HarmonyPrefix]
  public static bool HasJobOnCell_Prefix(Map ___map, IntVec3 c, ref bool __result)
  {
    if (___map == null) return true;

    if (RoofBuildings.HasNonMinifiableRoofBuildingAt(___map, c))
    {
      __result = false;
      return false;
    }
    return true;
  }
}
