using System;
using System.Collections.Generic;
using RimWorld;
using Verse;

using SolarWeb.Stratum.DefModExtensions;
using SolarWeb.Stratum.Stats;
using SolarWeb.Stratum.Hooks;
using SolarWeb.Stratum.Utilities;

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
  internal UnityEngine.Color?[] GlassTintsArray => glassTints;
  internal ThingDef?[] StuffDefsArray => stuffDefs;

  public override void ExposeData()
  {
    base.ExposeData();

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

      loadedDamagedCells = null;
      loadedSavedStuff = null;
      loadedSavedTints = null;
    }
  }

  public override void FinalizeInit()
  {
    base.FinalizeInit();
    InitializeNaturalRoofsStuff();

    if (!hasScanned)
    {
      ExecuteScan();
    }
    var registry = MapHookRegistry.Get(map);
    if (registry != null)
    {
      registry.Register<MapHookRegistry.RoofChangedHandler>(MapHookRegistry.HookId.RoofChanged, Notify_StratumRoofChanged);
    }
    if (map.areaManager != null)
    {
      map.areaManager.BuildRoof?.Clear();
      map.areaManager.NoRoof?.Clear();
    }
  }

  internal void InitializeNaturalRoofsStuff()
  {
    var numCells = map.cellIndices.NumGridCells;
    var roofGrid = map.roofGrid;
    for (int i = 0; i < numCells; i++)
    {
      var roof = roofGrid.RoofAt(i);
      if (roof != null && roof.isNatural)
      {
        var cell = map.cellIndices.IndexToCell(i);
        if (stuffDefs[i] == null)
        {
          stuffDefs[i] = GetStonyStuffForCell(roof, cell, map);
        }
      }
    }
  }



  public override void MapRemoved()
  {
    base.MapRemoved();
    var registry = MapHookRegistry.Get(map);
    if (registry != null)
    {
      registry.Unregister<MapHookRegistry.RoofChangedHandler>(MapHookRegistry.HookId.RoofChanged, Notify_StratumRoofChanged);
    }
  }

  private void Notify_StratumRoofChanged(Map m, IntVec3 c, RoofDef? oldRoof, RoofDef? newRoof)
  {
    if (m != map) return;

    try
    {
      if (Find.Selector != null && Find.Selector.SelectedObjects != null && Find.Selector.SelectedObjects.Count > 0)
      {
        for (int i = Find.Selector.SelectedObjects.Count - 1; i >= 0; i--)
        {
          var obj = Find.Selector.SelectedObjects[i];
          if (obj is UI.SelectedRoof sr && sr.map == map && sr.cell == c)
          {
            if (newRoof == null || sr.def != newRoof)
            {
              Find.Selector.Deselect(sr);
            }
          }
        }
      }
    }
    catch (Exception ex)
    {
      StratumLog.Error($"Error in RoofIntegrityGrid Notify_StratumRoofChanged selection cleanup: {ex}");
    }

    if (map.areaManager != null)
    {
      if (map.areaManager.NoRoof != null) map.areaManager.NoRoof[c] = false;
      if (map.areaManager.BuildRoof != null) map.areaManager.BuildRoof[c] = false;
    }

    if (map.regionAndRoomUpdater != null && map.regionAndRoomUpdater.Enabled)
    {
      var room = c.GetRoom(map);
      if (room != null && room.Districts != null)
      {
        foreach (var district in room.Districts)
        {
          if (district != null)
          {
            district.Notify_RoofChanged();
          }
        }
      }
    }

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
      if (oldRoof != null && RoofStatCache.IsCustomRoof(oldRoof))
      {
        if (RoofBuildings.isDeconstructingRoof)
        {
          var stuff = GetStuff(c);
          var ext = oldRoof.GetModExtension<BuildableRoofExtension>();
          if (ext != null && ext.buildableDef != null)
          {
            var costList = ext.buildableDef.CostListAdjusted(stuff);
            if (costList != null)
            {
              float refundFraction = ext.buildableDef.resourcesFractionWhenDeconstructed;
              foreach (var cost in costList)
              {
                int count = GenMath.RoundRandom(cost.count * refundFraction);
                if (count > 0)
                {
                  var deconstructItem = ThingMaker.MakeThing(cost.thingDef);
                  deconstructItem.stackCount = count;
                  GenPlace.TryPlaceThing(deconstructItem, c, map, ThingPlaceMode.Near);
                }
              }
            }
          }
        }
      }
      RemoveRoof(c);
    }
  }

  public void ExecuteScan(bool force = false)
  {
    lock (scanLockInt)
    {
      if (hasScanned && !force) return;
      InitializeNaturalRoofsStuff();
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

  public void SetHitPoints(IntVec3 cell, short hp)
  {
    if (!cell.InBounds(map)) return;
    int index = map.cellIndices.CellToIndex(cell);
    var roof = map.roofGrid.RoofAt(cell);
    if (roof == null) return;

    short maxHP = GetMaxHitPoints(cell);
    hitPoints[index] = (short)UnityEngine.Mathf.Clamp(hp, 0, maxHP);

    if (hitPoints[index] < maxHP)
      roofsNeedingRepair.Add(index);
    else
      roofsNeedingRepair.Remove(index);

    map.mapDrawer.MapMeshDirty(cell, MapMeshFlagDefOf.Roofs);
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
    int maxHp = RoofStatCache.GetMaxHitPoints(roof, GetStuff(cell));
    maxHp = MapHookRegistry.GetCellRoofMaxHitPoints(map, cell, maxHp);
    return (short)maxHp;
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

  public UnityEngine.Color? GetGlassTint(int index)
  {
    if (index < 0 || index >= glassTints.Length) return null;
    return glassTints[index];
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
    dt = MapHookRegistry.GetCellRoofDamageThreshold(map, cell, dt);

    float ar = RoofStatCache.GetArmorRating(roof, stuff);
    ar = MapHookRegistry.GetCellRoofArmorRating(map, cell, ar);

    float effectiveDamage = amount;

    bool handled = false;
    if (MapHookRegistry.Get(map)?.GetHandlers<MapHookRegistry.RoofDamageCalculationHandler>(MapHookRegistry.HookId.RoofDamageCalculation) is List<MapHookRegistry.RoofDamageCalculationHandler> handlers)
    {
      foreach (var handler in handlers)
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
        if (Find.PlaySettings != null && Find.PlaySettings.autoRebuild && map.areaManager?.Home != null && map.areaManager.Home[cell])
        {
          map.GetComponent<RoofConstructionTracker>()?.RebuildRoof(cell, roof, ext, stuff, tint);
        }

        int debrisCount = Rand.RangeInclusive(1, 2);
        for (int i = 0; i < debrisCount; i++)
        {
          if (roof.collapseLeavingThingDef == null) continue;

          var debris = ThingMaker.MakeThing(roof.collapseLeavingThingDef);
          GenPlace.TryPlaceThing(debris, cell, map, ThingPlaceMode.Near);
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
    if (map == null || !cell.InBounds(map)) return;
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

  public static ThingDef GetStonyStuffForTerrain(RoofDef roof, TerrainDef floor)
  {
    if (floor == null || roof == null)
    {
      return DefDatabase<ThingDef>.GetNamed("BlocksGranite");
    }

    var ext = roof.GetModExtension<BuildableRoofExtension>();
    if (ext != null && ext.terrainToStuff.TryGetValue(floor, out var stuff))
    {
      return stuff;
    }

    // Secondary fallback: check if any custom roof extension has this mapping
    foreach (var rDef in DefDatabase<RoofDef>.AllDefs)
    {
      var rExt = rDef.GetModExtension<BuildableRoofExtension>();
      if (rExt != null && rExt.terrainToStuff.TryGetValue(floor, out stuff))
      {
        return stuff;
      }
    }

    return DefDatabase<ThingDef>.GetNamed("BlocksGranite");
  }

  public static ThingDef GetStonyStuffForCell(RoofDef roof, IntVec3 cell, Map map)
  {
    if (cell.InBounds(map))
    {
      var edifice = cell.GetEdifice(map);
      if (edifice != null && edifice.def.building != null && edifice.def.building.isNaturalRock)
      {
        var blocksDef = GetStonyStuffForRock(edifice.def);
        if (blocksDef != null) return blocksDef;
      }

      var floor = cell.GetTerrain(map);
      if (floor != null)
      {
        var stuff = GetStonyStuffForTerrain(roof, floor);
        if (stuff != null) return stuff;
      }
    }

    return DefDatabase<ThingDef>.GetNamed("BlocksGranite");
  }

  private static ThingDef? GetStonyStuffForRock(ThingDef rockDef)
  {
    ThingDef? blocks = GetStonyStuffFromButcherProducts(rockDef);
    if (blocks != null) return blocks;

    if (rockDef.building?.mineableThing != null)
    {
      blocks = GetStonyStuffFromButcherProducts(rockDef.building.mineableThing);
      if (blocks != null) return blocks;
    }

    return null;
  }

  private static ThingDef? GetStonyStuffFromButcherProducts(ThingDef def)
  {
    if (def.butcherProducts != null)
    {
      foreach (var product in def.butcherProducts)
      {
        if (product.thingDef?.stuffProps?.categories?.Contains(StuffCategoryDefOf.Stony) == true)
        {
          return product.thingDef;
        }
      }
    }
    return null;
  }
}
