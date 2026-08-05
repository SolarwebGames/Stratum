using HarmonyLib;
using Verse;

using SolarWeb.Stratum.MapComponents;

namespace SolarWeb.Stratum.MultiFloors.Patches.MultiFloors.HarmonyPatches;

[HarmonyPatch(typeof(global::MultiFloors.HarmonyPatches.HarmonyPatch_NotifyRoofCollapse))]
public static class NotifyRoofCollapse_Patch
{
  [HarmonyPatch("SetupUpperLevelTerrain")]
  [HarmonyPrefix]
  public static bool SetupUpperLevelTerrain_Prefix(Map __0, IntVec3 __1, RoofDef __2)
  {
    if (__2 == null && __0 != null)
    {
      var tracker = __0.GetComponent<RetractableRoofTracker>();
      if (tracker != null && tracker.IsRetracted(__0.cellIndices.CellToIndex(__1)))
      {
        return false;
      }
    }
    return true;
  }
}
