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
  private readonly UnityEngine.Color?[] glassTints = new UnityEngine.Color?[map.cellIndices.NumGridCells];
  private readonly HashSet<int> roofsNeedingRepair = [];
  public bool hasScanned;
  private object scanLockInt = new();

  private Dictionary<int, short>? loadedDamagedCells;
  private Dictionary<int, ThingDef>? loadedSavedStuff;
  private Dictionary<int, UnityEngine.Color>? loadedSavedTints;

  public HashSet<int> RoofsNeedingRepair => roofsNeedingRepair;
  internal short[] HitPointsArray => hitPoints;

  public override void ExposeData()
  {
    base.ExposeData();
    Scribe_Values.Look(ref hasScanned, "hasScanned", false);

    if (Scribe.mode == LoadSaveMode.Saving)
    {
      loadedDamagedCells = [];
      loadedSavedStuff = [];
      loadedSavedTints = [];
      for (int i = 0; i < hitPoints.Length; i++)
      {
        if (roofsNeedingRepair.Contains(i))
          loadedDamagedCells[i] = hitPoints[i];

        if (stuffDefs[i] != null)
          loadedSavedStuff[i] = stuffDefs[i]!;

        if (glassTints[i] != null)
          loadedSavedTints[i] = glassTints[i]!.Value;
      }
    }

    Scribe_Collections.Look(ref loadedDamagedCells, "damagedCells", LookMode.Value, LookMode.Value);
    Scribe_Collections.Look(ref loadedSavedStuff, "savedStuff", LookMode.Value, LookMode.Def);
    Scribe_Collections.Look(ref loadedSavedTints, "savedTints", LookMode.Value, LookMode.Value);

    if (Scribe.mode == LoadSaveMode.PostLoadInit)
    {
      scanLockInt = new object();

      if (loadedDamagedCells != null)
      {
        foreach (var kvp in loadedDamagedCells)
        {
          hitPoints[kvp.Key] = kvp.Value;
          roofsNeedingRepair.Add(kvp.Key);
        }
      }

      if (loadedSavedStuff != null)
      {
        foreach (var kvp in loadedSavedStuff)
        {
          stuffDefs[kvp.Key] = kvp.Value;
        }
      }

      if (loadedSavedTints != null)
      {
        foreach (var kvp in loadedSavedTints)
        {
          glassTints[kvp.Key] = kvp.Value;
        }
      }

      // Clear memory after load completes
      loadedDamagedCells = null;
      loadedSavedStuff = null;
      loadedSavedTints = null;
    }
  }

  public override void FinalizeInit()
  {
    base.FinalizeInit();
    if (!hasScanned)
    {
      ExecuteScan();
    }
    Utilities.StratumHooks.OnRoofChanged += Notify_StratumRoofChanged;
    if (map.areaManager != null)
    {
      map.areaManager.BuildRoof?.Clear();
      map.areaManager.NoRoof?.Clear();
    }
  }

  public override void MapRemoved()
  {
    base.MapRemoved();
    Utilities.StratumHooks.OnRoofChanged -= Notify_StratumRoofChanged;
  }

  private void Notify_StratumRoofChanged(Map m, IntVec3 c, RoofDef? oldRoof, RoofDef? newRoof)
  {
    if (m != map) return;

    if (newRoof != null && RoofStatCache.IsCustomRoof(newRoof))
    {
      ThingDef? stuff = null;
      UnityEngine.Color? tint = null;
      if (DebugSettings.godMode)
      {
        var designator = Find.DesignatorManager.SelectedDesignator as AI.Designators.BuildCustomRoof;
        if (designator != null)
        {
          stuff = designator.StuffDef;
          tint = designator.SelectedTint;
        }
      }

      if (stuff == null && Patches.GravshipPlacementUtility_SpawnRoofs_Patch.CurrentLandingGravship != null)
      {
        var local = c - Patches.GravshipPlacementUtility_SpawnRoofs_Patch.CurrentLandingRoot;
        if (Patches.GravshipPlacementUtility_SpawnRoofs_Patch.CurrentRoofData != null &&
            Patches.GravshipPlacementUtility_SpawnRoofs_Patch.CurrentRoofData.TryGetValue(local, out var cellData))
        {
          stuff = cellData.stuff;
          InitializeRoof(c, newRoof, stuff, cellData.glassTint, cellData.hitPoints);
          return;
        }
      }

      InitializeRoof(c, newRoof, stuff, tint);
    }
    else
    {
      RemoveRoof(c);
    }
  }

  public void ExecuteScan(bool force = false)
  {
    lock (scanLockInt)
    {
      if (hasScanned && !force) return;
      hasScanned = true;
      ParallelMapScanner.ExecuteScan(this, force);
    }
  }

  public override void MapComponentUpdate()
  {
  }

  public void InitializeRoof(IntVec3 cell, RoofDef def, ThingDef? stuff = null, UnityEngine.Color? glassTint = null, short? currentHP = null)
  {
    if (!cell.InBounds(map)) return;
    int index = map.cellIndices.CellToIndex(cell);
    if (RoofStatCache.IsCustomRoof(def))
    {
      short maxHP = (short)RoofStatCache.GetMaxHitPoints(def, stuff);
      hitPoints[index] = currentHP ?? maxHP;
      stuffDefs[index] = stuff;
      glassTints[index] = glassTint;

      if (hitPoints[index] < maxHP)
        roofsNeedingRepair.Add(index);
      else
        roofsNeedingRepair.Remove(index);
    }
  }

  public void RemoveRoof(IntVec3 cell)
  {
    if (!cell.InBounds(map)) return;
    int index = map.cellIndices.CellToIndex(cell);
    hitPoints[index] = 0;
    stuffDefs[index] = null;
    glassTints[index] = null;
    roofsNeedingRepair.Remove(index);
  }

  public short GetHitPoints(IntVec3 cell)
  {
    if (!cell.InBounds(map)) return 0;
    return hitPoints[map.cellIndices.CellToIndex(cell)];
  }

  public float GetEffectiveInsulation(IntVec3 cell)
  {
    if (!cell.InBounds(map)) return 0f;
    var roof = map.roofGrid.RoofAt(cell);
    if (roof == null) return 0f;
    return RoofStatCache.GetEffectiveInsulation(roof, GetStuff(cell));
  }

  public short GetMaxHitPoints(IntVec3 cell)
  {
    if (!cell.InBounds(map)) return 0;
    var roof = map.roofGrid.RoofAt(cell);
    if (roof == null) return 0;
    return (short)RoofStatCache.GetMaxHitPoints(roof, GetStuff(cell));
  }

  public ThingDef? GetStuff(IntVec3 cell)
  {
    if (!cell.InBounds(map)) return null;
    return stuffDefs[map.cellIndices.CellToIndex(cell)];
  }

  public UnityEngine.Color? GetGlassTint(IntVec3 cell)
  {
    if (!cell.InBounds(map)) return null;
    return glassTints[map.cellIndices.CellToIndex(cell)];
  }

  public void TakeDamage(IntVec3 cell, float amount, float penetration = 0f, DamageInfo? dinfo = null)
  {
    if (dinfo != null && !dinfo.Value.Def.harmsHealth) return;

    if (!cell.InBounds(map)) return;
    int index = map.cellIndices.CellToIndex(cell);
    if (hitPoints[index] <= 0) return;

    var roof = map.roofGrid.RoofAt(cell);
    if (roof == null) return;

    var stuff = stuffDefs[index];
    float dt = RoofStatCache.GetDamageThreshold(roof, stuff);
    float ar = RoofStatCache.GetArmorRating(roof, stuff);

    float effectiveDamage = amount;

    bool handled = false;
    if (Utilities.StratumHooks.OnCalculateDamage != null)
    {
      foreach (Utilities.StratumHooks.RoofDamageCalculationHandler handler in Utilities.StratumHooks.OnCalculateDamage.GetInvocationList())
      {
        if (handler(roof, stuff, amount, penetration, dinfo, ref effectiveDamage))
        {
          handled = true;
          break;
        }
      }
    }

    if (!handled)
    {
      effectiveDamage -= dt;
      if (effectiveDamage <= 0) return;

      float effectiveArmor = System.Math.Max(0f, ar - penetration);
      effectiveDamage *= (1f - effectiveArmor);
    }

    if (effectiveDamage <= 0) return;

    int finalDamage = GenMath.RoundRandom(effectiveDamage);
    if (finalDamage <= 0) return;

    var maxHP = GetMaxHitPoints(cell);
    hitPoints[index] -= (short)finalDamage;

    if (hitPoints[index] <= 0)
    {
      hitPoints[index] = 0;
      var tint = glassTints[index];
      stuffDefs[index] = null;
      glassTints[index] = null;
      roofsNeedingRepair.Remove(index);

      RoofCollapserImmediate.DropRoofInCells(cell, map);

      map.mapDrawer.MapMeshDirty(cell, MapMeshFlagDefOf.Roofs);

      var ext = roof.GetModExtension<BuildableRoofExtension>();
      if (ext != null)
      {
        if (Find.PlaySettings.autoRebuild && map.areaManager.Home[cell])
        {
          map.GetComponent<RoofConstructionTracker>()?.RebuildRoof(cell, roof, ext, stuff, tint);
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
