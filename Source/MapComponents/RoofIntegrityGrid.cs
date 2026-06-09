using System.Collections.Generic;
using RimWorld;
using Verse;

using SolarWeb.Stratum.DefModExtensions;
using SolarWeb.Stratum.Stats;

namespace SolarWeb.Stratum.MapComponents;

public class RoofIntegrityGrid(Map map) : MapComponent(map)
{
  private readonly short[] hitPoints = new short[map.cellIndices.NumGridCells];
  private readonly ThingDef?[] stuffDefs = new ThingDef[map.cellIndices.NumGridCells];
  private readonly HashSet<int> roofsNeedingRepair = [];
  public bool hasScanned;

  public HashSet<int> RoofsNeedingRepair => roofsNeedingRepair;
  internal short[] HitPointsArray => hitPoints;

  public override void ExposeData()
  {
    base.ExposeData();

    // We only save cells that have missing hitpoints to save space.
    Dictionary<int, short>? damagedCells = null;
    Dictionary<int, ThingDef>? savedStuff = null;

    if (Scribe.mode == LoadSaveMode.Saving)
    {
      damagedCells = [];
      savedStuff = [];
      for (int i = 0; i < hitPoints.Length; i++)
      {
        if (roofsNeedingRepair.Contains(i))
          damagedCells[i] = hitPoints[i];

        if (stuffDefs[i] != null)
          savedStuff[i] = stuffDefs[i]!;
      }
    }

    Scribe_Collections.Look(ref damagedCells, "damagedCells", LookMode.Value, LookMode.Value);
    Scribe_Collections.Look(ref savedStuff, "savedStuff", LookMode.Value, LookMode.Def);

    if (Scribe.mode == LoadSaveMode.LoadingVars)
    {
      if (damagedCells != null)
      {
        foreach (var kvp in damagedCells)
        {
          hitPoints[kvp.Key] = kvp.Value;
          roofsNeedingRepair.Add(kvp.Key);
        }
      }

      if (savedStuff != null)
      {
        foreach (var kvp in savedStuff)
        {
          stuffDefs[kvp.Key] = kvp.Value;
        }
      }
    }
  }

  public override void MapComponentUpdate()
  {
    // Defer scanning until the overlay is actually requested.
    // This ensures map generation is completely finished.
    if (!hasScanned && Find.PlaySettings.showRoofOverlay)
    {
      ParallelMapScanner.ExecuteScan(map);
    }
  }

  public void InitializeRoof(IntVec3 cell, RoofDef def, ThingDef? stuff = null)
  {
    int index = map.cellIndices.CellToIndex(cell);
    if (RoofStatCache.IsCustomRoof(def))
    {
      hitPoints[index] = (short)RoofStatCache.GetMaxHitPoints(def, stuff);
      stuffDefs[index] = stuff;
      roofsNeedingRepair.Remove(index);
    }
  }

  public void RemoveRoof(IntVec3 cell)
  {
    int index = map.cellIndices.CellToIndex(cell);
    hitPoints[index] = 0;
    stuffDefs[index] = null;
    roofsNeedingRepair.Remove(index);
  }

  public short GetHitPoints(IntVec3 cell)
  {
    return hitPoints[map.cellIndices.CellToIndex(cell)];
  }

  public short GetMaxHitPoints(IntVec3 cell)
  {
    var roof = map.roofGrid.RoofAt(cell);
    if (roof == null) return 0;
    return (short)RoofStatCache.GetMaxHitPoints(roof, GetStuff(cell));
  }

  public ThingDef? GetStuff(IntVec3 cell)
  {
    return stuffDefs[map.cellIndices.CellToIndex(cell)];
  }

  public void TakeDamage(IntVec3 cell, int amount)
  {
    int index = map.cellIndices.CellToIndex(cell);
    if (hitPoints[index] <= 0) return;

    var maxHP = GetMaxHitPoints(cell);
    hitPoints[index] -= (short)amount;

    if (hitPoints[index] <= 0)
    {
      hitPoints[index] = 0;
      var stuff = stuffDefs[index];
      stuffDefs[index] = null;
      roofsNeedingRepair.Remove(index);

      var roofDef = map.roofGrid.RoofAt(cell);
      RoofCollapserImmediate.DropRoofInCells(cell, map);

      map.mapDrawer.MapMeshDirty(cell, MapMeshFlagDefOf.Roofs);

      var ext = roofDef.GetModExtension<BuildableRoofExtension>();
      if (ext != null)
      {
        if (Find.PlaySettings.autoRebuild && map.areaManager.Home[cell])
        {
          map.GetComponent<RoofConstructionTracker>()?.RebuildRoof(cell, roofDef, ext, stuff);
        }

        var bDef = ext.buildableDef;
        if (bDef != null && !bDef.CostList.NullOrEmpty())
        {
          float fraction = bDef.resourcesFractionWhenDeconstructed;
          var costList = bDef.CostListAdjusted(stuff);
          foreach (var cost in costList)
          {
            int count = GenMath.RoundRandom(cost.count * fraction);
            if (count > 0)
            {
              var thing = ThingMaker.MakeThing(cost.thingDef);
              thing.stackCount = count;
              GenPlace.TryPlaceThing(thing, cell, map, ThingPlaceMode.Near);
            }
          }
        }
      }
    }
    else if (hitPoints[index] < maxHP)
    {
      roofsNeedingRepair.Add(index);
      map.mapDrawer.MapMeshDirty(cell, MapMeshFlagDefOf.Roofs);
    }
  }

  public void Repair(IntVec3 cell, int amount)
  {
    int index = map.cellIndices.CellToIndex(cell);
    var maxHP = GetMaxHitPoints(cell);

    if (hitPoints[index] > 0 && hitPoints[index] < maxHP)
    {
      hitPoints[index] += (short)amount;
      if (hitPoints[index] >= maxHP)
      {
        hitPoints[index] = maxHP;
        roofsNeedingRepair.Remove(index);
      }
      map.mapDrawer.MapMeshDirty(cell, MapMeshFlagDefOf.Roofs);
    }
  }
}
