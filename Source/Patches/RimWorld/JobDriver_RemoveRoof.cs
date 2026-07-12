using System.Runtime.CompilerServices;
using HarmonyLib;
using RimWorld;

using SolarWeb.Stratum.Utilities;

namespace SolarWeb.Stratum.Patches;

[HarmonyPatch(typeof(JobDriver_RemoveRoof))]
public static class JobDriver_RemoveRoof_Patch
{
  private class JobStartState
  {
    public bool HadBuilding;
  }

  private static readonly ConditionalWeakTable<JobDriver_RemoveRoof, JobStartState> jobStartBuildings =
    new ConditionalWeakTable<JobDriver_RemoveRoof, JobStartState>();

  [HarmonyPatch("MakeNewToils")]
  [HarmonyPrefix]
  public static void MakeNewToils_Prefix(JobDriver_RemoveRoof __instance)
  {
    if (__instance.pawn != null && __instance.pawn.Map != null && __instance.job != null)
    {
      var map = __instance.pawn.Map;
      var cell = __instance.job.targetA.Cell;
      bool hadBuilding = RoofBuildings.HasConstructedRoofBuildingAt(map, cell);
      jobStartBuildings.AddOrUpdate(__instance, new JobStartState { HadBuilding = hadBuilding });
    }
  }

  [HarmonyPatch("DoEffect")]
  [HarmonyPrefix]
  public static void DoEffect_Prefix()
  {
    RoofBuildings.isDeconstructingRoof = true;
  }

  [HarmonyPatch("DoEffect")]
  [HarmonyPostfix]
  public static void DoEffect_Postfix()
  {
    RoofBuildings.isDeconstructingRoof = false;
  }

  [HarmonyPatch("DoWorkFailOn")]
  [HarmonyPostfix]
  public static void DoWorkFailOn_Postfix(JobDriver_RemoveRoof __instance, ref bool __result)
  {
    if (__result) return;

    if (__instance.pawn != null && __instance.pawn.Map != null && __instance.job != null)
    {
      var map = __instance.pawn.Map;
      var cell = __instance.job.targetA.Cell;

      if (RoofBuildings.HasNonMinifiableRoofBuildingAt(map, cell))
      {
        __result = true;
        return;
      }

      if (jobStartBuildings.TryGetValue(__instance, out var state))
      {
        if (!state.HadBuilding && RoofBuildings.HasConstructedRoofBuildingAt(map, cell))
        {
          __result = true;
        }
      }
    }
  }
}

