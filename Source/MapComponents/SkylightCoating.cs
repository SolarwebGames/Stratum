using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

using SolarWeb.Stratum.Hooks;

namespace SolarWeb.Stratum.MapComponents;

public class SkylightCoating(Map map) : MapComponent(map)
{
  private readonly float[] dirtLevels = new float[map.cellIndices.NumGridCells];
  private readonly Color[] dirtColors = new Color[map.cellIndices.NumGridCells];
  private readonly float[] pollenLevels = new float[map.cellIndices.NumGridCells];
  private readonly float[] snowLevels = new float[map.cellIndices.NumGridCells];
  private readonly HashSet<int> activeSkylightCells = [];
  private readonly HashSet<int> activeVisibleRoofedCells = [];

  private Dictionary<int, float>? loadedDirtLevels;
  private Dictionary<int, Color>? loadedDirtColors;
  private Dictionary<int, float>? loadedPollenLevels;
  private Dictionary<int, float>? loadedSnowLevels;
  private bool hasScanned = false;

  public override void FinalizeInit()
  {
    base.FinalizeInit();
    MapHookRegistry.Get(map)?.Register<MapHookRegistry.RoofChangedHandler>(MapHookRegistry.HookId.RoofChanged, Notify_StratumRoofChanged);
  }

  private void ExecuteScan()
  {
    hasScanned = true;
    activeSkylightCells.Clear();
    activeVisibleRoofedCells.Clear();

    for (int i = 0; i < map.cellIndices.NumGridCells; i++)
    {
      RoofDef roof = map.roofGrid.RoofAt(i);
      if (roof != null && Stats.RoofStatCache.IsVisibleRoof(roof))
      {
        activeVisibleRoofedCells.Add(i);
        if (Stats.RoofStatCache.IsSkylight(roof))
        {
          activeSkylightCells.Add(i);
        }
      }
    }
  }

  public override void MapRemoved()
  {
    base.MapRemoved();
    MapHookRegistry.Get(map)?.Unregister<MapHookRegistry.RoofChangedHandler>(MapHookRegistry.HookId.RoofChanged, Notify_StratumRoofChanged);
  }

  private void Notify_StratumRoofChanged(Map m, IntVec3 c, RoofDef? oldRoof, RoofDef? newRoof)
  {
    if (m != map) return;
    int idx = map.cellIndices.CellToIndex(c);

    dirtLevels[idx] = 0f;
    dirtColors[idx] = Color.white;
    pollenLevels[idx] = 0f;
    snowLevels[idx] = 0f;

    bool isNewSkylight = newRoof != null && Stats.RoofStatCache.IsSkylight(newRoof);
    if (isNewSkylight)
    {
      activeSkylightCells.Add(idx);
    }
    else
    {
      activeSkylightCells.Remove(idx);
    }

    bool isVisible = newRoof != null && Stats.RoofStatCache.IsVisibleRoof(newRoof);
    if (isVisible)
    {
      activeVisibleRoofedCells.Add(idx);
    }
    else
    {
      activeVisibleRoofedCells.Remove(idx);
    }
  }

  public float GetDirtLevel(IntVec3 cell)
  {
    if (!cell.InBounds(map)) return 0f;
    return dirtLevels[map.cellIndices.CellToIndex(cell)];
  }

  public Color GetDirtColor(IntVec3 cell)
  {
    if (!cell.InBounds(map)) return Color.white;
    return dirtColors[map.cellIndices.CellToIndex(cell)];
  }

  public float GetPollenLevel(IntVec3 cell)
  {
    if (!cell.InBounds(map)) return 0f;
    return pollenLevels[map.cellIndices.CellToIndex(cell)];
  }

  public float GetSnowLevel(IntVec3 cell)
  {
    if (!cell.InBounds(map)) return 0f;
    return snowLevels[map.cellIndices.CellToIndex(cell)];
  }

  public float GetCoatingOpacity(IntVec3 cell)
  {
    if (!cell.InBounds(map)) return 0f;
    int idx = map.cellIndices.CellToIndex(cell);
    return Mathf.Clamp01(dirtLevels[idx] + pollenLevels[idx] + snowLevels[idx]);
  }

  public void SetDirtLevel(IntVec3 cell, float level)
  {
    if (!cell.InBounds(map)) return;
    int idx = map.cellIndices.CellToIndex(cell);
    dirtLevels[idx] = Mathf.Clamp01(level);
    map.mapDrawer.MapMeshDirty(cell, MapMeshFlagDefOf.Roofs);
  }

  public void SetDirtLevel(IntVec3 cell, float level, Color color)
  {
    if (!cell.InBounds(map)) return;
    int idx = map.cellIndices.CellToIndex(cell);
    dirtLevels[idx] = Mathf.Clamp01(level);
    dirtColors[idx] = color;
    map.mapDrawer.MapMeshDirty(cell, MapMeshFlagDefOf.Roofs);
  }

  public void SetPollenLevel(IntVec3 cell, float level)
  {
    if (!cell.InBounds(map)) return;
    int idx = map.cellIndices.CellToIndex(cell);
    pollenLevels[idx] = Mathf.Clamp01(level);
    map.mapDrawer.MapMeshDirty(cell, MapMeshFlagDefOf.Roofs);
  }

  public void SetSnowLevel(IntVec3 cell, float level)
  {
    if (!cell.InBounds(map)) return;
    int idx = map.cellIndices.CellToIndex(cell);
    snowLevels[idx] = Mathf.Clamp01(level);
    map.mapDrawer.MapMeshDirty(cell, MapMeshFlagDefOf.Roofs);
  }

  public override void MapComponentTick()
  {
    base.MapComponentTick();

    if (!hasScanned)
    {
      ExecuteScan();
    }

    if (Find.TickManager.TicksGame % 250 != 0) return;

    AccumulateNaturalDirt();
    WashDirtWithRain();
  }

  private void AccumulateNaturalDirt()
  {
    int cellsToTick = Mathf.Max(1, map.Area / 2000);
    Season season = GenLocalDate.Season(map);
    bool isPollenSeason = (season == Season.Spring || season == Season.Summer);

    for (int k = 0; k < cellsToTick; k++)
    {
      IntVec3 cell = new(Rand.Range(0, map.Size.x), 0, Rand.Range(0, map.Size.z));
      RoofDef roof = map.roofGrid.RoofAt(cell);
      if (roof != null && Stats.RoofStatCache.IsSkylight(roof))
      {
        int idx = map.cellIndices.CellToIndex(cell);
        if (isPollenSeason)
        {
          float curPollen = pollenLevels[idx];
          if (curPollen < 1f)
          {
            pollenLevels[idx] = Mathf.Min(1f, curPollen + 0.05f);
            map.mapDrawer.MapMeshDirty(cell, MapMeshFlagDefOf.Roofs);
          }
        }
        else
        {
          float curDirt = dirtLevels[idx];
          if (curDirt < 1f)
          {
            dirtLevels[idx] = Mathf.Min(1f, curDirt + 0.05f);
            map.mapDrawer.MapMeshDirty(cell, MapMeshFlagDefOf.Roofs);
          }
        }
      }
    }

    // Compounding dirt/pollen accumulation
    foreach (int idx in activeSkylightCells)
    {
      float curDirt = dirtLevels[idx];
      if (curDirt > 0.01f && curDirt < 1f)
      {
        if (Rand.Value < curDirt * 0.10f)
        {
          dirtLevels[idx] = Mathf.Min(1f, curDirt + 0.05f);
          map.mapDrawer.MapMeshDirty(map.cellIndices.IndexToCell(idx), MapMeshFlagDefOf.Roofs);
        }
      }

      float curPollen = pollenLevels[idx];
      if (curPollen > 0.01f && curPollen < 1f)
      {
        if (Rand.Value < curPollen * 0.10f)
        {
          pollenLevels[idx] = Mathf.Min(1f, curPollen + 0.05f);
          map.mapDrawer.MapMeshDirty(map.cellIndices.IndexToCell(idx), MapMeshFlagDefOf.Roofs);
        }
      }
    }
  }

  private void WashDirtWithRain()
  {
    float rain = (map.mapTemperature.OutdoorTemp > 0f) ? map.weatherManager.RainRate : 0f;
    if (rain > 0.1f)
    {
      foreach (int idx in activeSkylightCells)
      {
        bool dirty = false;
        if (dirtLevels[idx] > 0.001f)
        {
          dirtLevels[idx] = Mathf.Max(0f, dirtLevels[idx] - 0.05f);
          dirty = true;
        }
        if (pollenLevels[idx] > 0.001f)
        {
          pollenLevels[idx] = Mathf.Max(0f, pollenLevels[idx] - 0.05f);
          dirty = true;
        }
        if (dirty)
        {
          map.mapDrawer.MapMeshDirty(map.cellIndices.IndexToCell(idx), MapMeshFlagDefOf.Roofs);
        }
      }
    }
  }

  public Color GetSeasonalDirtColor()
  {
    Season season = GenLocalDate.Season(map);
    if (season == Season.Spring || season == Season.Summer)
    {
      return new Color(0.3f, 0.28f, 0.1f);
    }
    else
    {
      return new Color(0.22f, 0.18f, 0.13f);
    }
  }

  public override void ExposeData()
  {
    base.ExposeData();

    if (Scribe.mode == LoadSaveMode.Saving)
    {
      loadedDirtLevels = [];
      loadedDirtColors = [];
      loadedPollenLevels = [];
      loadedSnowLevels = [];
      for (int i = 0; i < dirtLevels.Length; i++)
      {
        if (dirtLevels[i] > 0.001f)
        {
          loadedDirtLevels[i] = dirtLevels[i];
          loadedDirtColors[i] = dirtColors[i];
        }
        if (pollenLevels[i] > 0.001f)
        {
          loadedPollenLevels[i] = pollenLevels[i];
        }
        if (snowLevels[i] > 0.001f)
        {
          loadedSnowLevels[i] = snowLevels[i];
        }
      }
    }

    Scribe_Collections.Look(ref loadedDirtLevels, "dirtLevels", LookMode.Value, LookMode.Value);
    Scribe_Collections.Look(ref loadedDirtColors, "dirtColors", LookMode.Value, LookMode.Value);
    Scribe_Collections.Look(ref loadedPollenLevels, "pollenLevels", LookMode.Value, LookMode.Value);
    Scribe_Collections.Look(ref loadedSnowLevels, "snowLevels", LookMode.Value, LookMode.Value);

    if (Scribe.mode == LoadSaveMode.PostLoadInit)
    {
      if (loadedDirtLevels != null)
      {
        foreach (var kvp in loadedDirtLevels)
        {
          dirtLevels[kvp.Key] = kvp.Value;
        }
      }
      if (loadedDirtColors != null)
      {
        foreach (var kvp in loadedDirtColors)
        {
          dirtColors[kvp.Key] = kvp.Value;
        }
      }
      if (loadedPollenLevels != null)
      {
        foreach (var kvp in loadedPollenLevels)
        {
          pollenLevels[kvp.Key] = kvp.Value;
        }
      }
      if (loadedSnowLevels != null)
      {
        foreach (var kvp in loadedSnowLevels)
        {
          snowLevels[kvp.Key] = kvp.Value;
        }
      }

      loadedDirtLevels = null;
      loadedDirtColors = null;
      loadedPollenLevels = null;
      loadedSnowLevels = null;
    }
  }
}
