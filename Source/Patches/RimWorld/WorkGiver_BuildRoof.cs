using HarmonyLib;
using RimWorld;

namespace SolarWeb.Stratum.Patches;

[HarmonyPatch]
public static class WorkGiver_BuildRoof_Patch
{
  [HarmonyPatch(typeof(WorkGiver_BuildRoof), nameof(WorkGiver_BuildRoof.HasJobOnCell))]
  [HarmonyPrefix]
  public static bool HasJobOnCell_Prefix(ref bool __result)
  {
    __result = false;
    return false; // Disable vanilla roof construction jobs
  }
}
