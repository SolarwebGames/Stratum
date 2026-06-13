using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using SolarWeb.Stratum.MapComponents;
using SolarWeb.Stratum.Stats;
using SolarWeb.Stratum.WorldComponents;
using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace SolarWeb.Stratum.Patches;

[HarmonyPatch]
public static class GravshipUtility_Patch
{
  private struct CapturedData
  {
    public Dictionary<IntVec3, GravshipRoofTracker.RoofCellData> roofs;
    public Dictionary<IntVec3, RoofConstructionTracker.ConstructionRecord> construction;
  }

  private static readonly Dictionary<Building_GravEngine, CapturedData> captureQueue = [];

  [HarmonyPatch(typeof(GravshipUtility), nameof(GravshipUtility.GenerateGravship))]
  [HarmonyPrefix]
  public static void GenerateGravship_Prefix(Building_GravEngine engine)
  {
    if (engine?.Map == null) return;

    var integrity = engine.Map.GetComponent<RoofIntegrityGrid>();
    if (integrity == null) return;

    var constructionTracker = engine.Map.GetComponent<RoofConstructionTracker>();

    var roofs = new Dictionary<IntVec3, GravshipRoofTracker.RoofCellData>();
    var construction = new Dictionary<IntVec3, RoofConstructionTracker.ConstructionRecord>();
    var origin = engine.Position;
    var map = engine.Map;

    foreach (var cell in engine.ValidSubstructure)
    {
      var local = cell - origin;
      var roof = map.roofGrid.RoofAt(cell);
      if (RoofStatCache.IsCustomRoof(roof))
      {
        roofs[local] = new GravshipRoofTracker.RoofCellData
        {
          stuff = integrity.GetStuff(cell),
          hitPoints = integrity.GetHitPoints(cell),
          glassTint = integrity.GetGlassTint(cell)
        };
      }

      if (constructionTracker != null && constructionTracker.TryGetRecord(cell, out var record))
      {
        construction[local] = record;
      }
    }

    if (roofs.Count > 0 || construction.Count > 0)
    {
      captureQueue[engine] = new CapturedData { roofs = roofs, construction = construction };
    }
  }

  [HarmonyPatch(typeof(GravshipUtility), nameof(GravshipUtility.GenerateGravship))]
  [HarmonyPostfix]
  public static void GenerateGravship_Postfix(Gravship __result, Building_GravEngine engine)
  {
    if (__result != null && captureQueue.TryGetValue(engine, out var data))
    {
      Find.World.GetComponent<GravshipRoofTracker>()?.RegisterGravship(__result.ID, data.roofs, data.construction);
      captureQueue.Remove(engine);
    }
  }
}

