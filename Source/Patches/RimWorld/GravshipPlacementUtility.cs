using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using SolarWeb.Stratum.MapComponents;
using SolarWeb.Stratum.WorldComponents;
using System.Collections.Generic;
using Verse;

namespace SolarWeb.Stratum.Patches;

[HarmonyPatch]
public static class GravshipPlacementUtility_SpawnRoofs_Patch
{
  public static Gravship? CurrentLandingGravship;
  public static IntVec3 CurrentLandingRoot;
  public static Dictionary<IntVec3, GravshipRoofTracker.RoofCellData>? CurrentRoofData;
  public static Dictionary<IntVec3, RoofConstructionTracker.ConstructionRecord>? CurrentConstructionData;

  [HarmonyPatch(typeof(GravshipPlacementUtility), "SpawnRoofs")]
  [HarmonyPrefix]
  public static void Prefix(Gravship gravship, IntVec3 root)
  {
    CurrentLandingGravship = gravship;
    CurrentLandingRoot = root;
    CurrentRoofData = [];
    CurrentConstructionData = null;

    if (Find.World.GetComponent<GravshipRoofTracker>().TryGetRoofData(gravship.ID, out var data))
    {
      CurrentConstructionData = data!.construction;
      foreach (var kvp in data.roofs)
      {
        CurrentRoofData[PrefabUtility.GetAdjustedLocalPosition(kvp.Key, gravship.Rotation)] = kvp.Value;
      }
    }
  }

  [HarmonyPatch(typeof(GravshipPlacementUtility), "SpawnRoofs")]
  [HarmonyPostfix]
  public static void Postfix(Gravship gravship, IntVec3 root, Map map)
  {
    if (CurrentConstructionData != null)
    {
      var tracker = map.GetComponent<RoofConstructionTracker>();
      if (tracker != null)
      {
        foreach (var kvp in CurrentConstructionData)
        {
          var targetCell = root + PrefabUtility.GetAdjustedLocalPosition(kvp.Key, gravship.Rotation);
          tracker.RestoreRecord(targetCell, kvp.Value);
        }
      }
    }

    CurrentLandingGravship = null;
    CurrentLandingRoot = IntVec3.Zero;
    CurrentRoofData = null;
    CurrentConstructionData = null;

    Find.World.GetComponent<GravshipRoofTracker>()?.UnregisterGravship(gravship.ID);
  }
}
