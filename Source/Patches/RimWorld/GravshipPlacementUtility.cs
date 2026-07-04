using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using System.Collections.Generic;
using Verse;

using SolarWeb.Stratum.MapComponents;
using SolarWeb.Stratum.WorldComponents;

namespace SolarWeb.Stratum.Patches;

[HarmonyPatch(typeof(GravshipPlacementUtility))]
public static class GravshipPlacementUtility_SpawnRoofs_Patch
{
  public static Gravship? CurrentLandingGravship;
  public static IntVec3 CurrentLandingRoot;
  public static Dictionary<IntVec3, GravshipRoofTracker.RoofCellData>? CurrentRoofData;
  public static Dictionary<IntVec3, RoofConstructionTracker.ConstructionRecord>? CurrentConstructionData;

  [HarmonyPatch("SpawnRoofs")]
  [HarmonyPrefix]
  public static void Prefix(Gravship gravship, IntVec3 root)
  {
    CurrentLandingGravship = gravship;
    CurrentLandingRoot = root;
    if (Find.World.GetComponent<GravshipRoofTracker>().TryGetRoofData(gravship.ID, out var data))
    {
      CurrentRoofData = [];
      foreach (var kvp in data!.roofs)
      {
        CurrentRoofData[PrefabUtility.GetAdjustedLocalPosition(kvp.Key, gravship.Rotation)] = kvp.Value;
      }

      CurrentConstructionData = [];
      foreach (var kvp in data.construction)
      {
        CurrentConstructionData[kvp.Key] = kvp.Value;
      }
    }
  }

  [HarmonyPatch("SpawnRoofs")]
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

    if (CurrentRoofData != null)
    {
      var integrityGrid = map.GetComponent<RoofIntegrityGrid>();
      var skylightCoating = map.GetComponent<SkylightCoating>();
      foreach (var kvp in CurrentRoofData)
      {
        if (kvp.Value.roofDef != null)
        {
          var targetCell = root + PrefabUtility.GetAdjustedLocalPosition(kvp.Key, gravship.Rotation);
          if (map.roofGrid.RoofAt(targetCell) == null)
          {
            map.roofGrid.SetRoof(targetCell, kvp.Value.roofDef);

            integrityGrid?.InitializeRoof(targetCell, kvp.Value.roofDef, kvp.Value.stuff, kvp.Value.glassTint ?? UnityEngine.Color.white, kvp.Value.hitPoints);

            skylightCoating?.SetSnowLevel(targetCell, 0f);
          }
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
