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

  public IReadOnlyCollection<int> ActiveSkylightCells
  {
    get
    {
      if (!hasScanned) ExecuteScan();
      return activeSkylightCells;
    }
  }

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

  public float GetDirtLevel(int idx)
  {
    if ((uint)idx >= (uint)dirtLevels.Length) return 0f;
    return dirtLevels[idx];
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

  public float GetSnowLevel(int idx)
  {
    if ((uint)idx >= (uint)snowLevels.Length) return 0f;
    return snowLevels[idx];
  }

  private float[]? snowNoiseCache;

  public float GetOrComputeSnowNoise(int idx, IntVec3 c, SteadyEnvironmentEffects effects)
  {
    snowNoiseCache ??= new float[map.cellIndices.NumGridCells];
    float val = snowNoiseCache[idx];
    if (val <= 0f)
    {
      var snowNoise = Patches.SteadyEnvironmentEffects_Patch.SnowNoiseRef(effects);
      if (snowNoise == null)
      {
        snowNoise = new Verse.Noise.Perlin(0.03999999910593033, 2.0, 0.5, 5, Rand.Range(0, 651431), Verse.Noise.QualityMode.Medium);
        Patches.SteadyEnvironmentEffects_Patch.SnowNoiseRef(effects) = snowNoise;
      }
      val = snowNoise.GetValue(c);
      val = Mathf.Max(0.5f, (val + 1f) * 0.5f);
      snowNoiseCache[idx] = val;
    }
    return val;
  }

  public float GetCoatingOpacity(IntVec3 cell)
  {
    if (!Stratum.Settings.enableSkylightCoating) return 0f;
    if (!cell.InBounds(map)) return 0f;
    return CoatingTotal(map.cellIndices.CellToIndex(cell));
  }

  public float GetLightBlockedFraction(IntVec3 cell)
    => Mathf.Clamp01(GetCoatingOpacity(cell) * Stratum.Settings.skylightCoatingLightPenalty);

  public const float CleanThreshold = 0.1f;

  private const float RoofQuantum = 0.05f;
  // Snow moves on every cell visit during a storm, where dirt and pollen move once per 250 ticks.
  // A Roofs dirty marks the cell's section plus all 8 neighbours' and rebuilds seven Stratum
  // SectionLayers, so snow gets a coarser step than the rest of the coating.
  private const float SnowQuantum = 0.125f;
  private const float GlowQuantum = 0.125f; // 8 steps: lighting reads only the clamped total, and a
                                            // 0.05 delta shifts overlay alpha by ~1 of 100

  private static bool CrossedQuantum(float oldValue, float newValue, float quantum)
    => (int)(oldValue / quantum) != (int)(newValue / quantum) || (oldValue > 0f) != (newValue > 0f);

  private void NotifyCoatingChanged(IntVec3 cell, float oldChannel, float newChannel, float oldTotal, float newTotal, float roofQuantum)
  {
    if (CrossedQuantum(oldChannel, newChannel, roofQuantum))
    {
      map.mapDrawer.MapMeshDirty(cell, MapMeshFlagDefOf.Roofs);
    }
    if (Stratum.Settings.enableSkylightCoating && CrossedQuantum(oldTotal, newTotal, GlowQuantum))
    {
      map.mapDrawer.MapMeshDirty(cell, MapMeshFlagDefOf.GroundGlow);
    }
  }

  private void NotifyCoatingChangedForced(IntVec3 cell)
  {
    map.mapDrawer.MapMeshDirty(cell, MapMeshFlagDefOf.Roofs);
    map.mapDrawer.MapMeshDirty(cell, MapMeshFlagDefOf.GroundGlow);
  }

  private float CoatingTotal(int idx) => Mathf.Clamp01(dirtLevels[idx] + pollenLevels[idx] + snowLevels[idx]);

  private void ApplyCoating(float[] channel, int idx, IntVec3 cell, float level, float roofQuantum = RoofQuantum)
  {
    if ((uint)idx >= (uint)channel.Length) return;
    float oldChannel = channel[idx];
    float newChannel = Mathf.Clamp01(level);
    if (oldChannel == newChannel) return;

    float oldTotal = CoatingTotal(idx);
    channel[idx] = newChannel;
    float newTotal = CoatingTotal(idx);

    NotifyCoatingChanged(cell, oldChannel, newChannel, oldTotal, newTotal, roofQuantum);
  }

  public void SetDirtLevel(IntVec3 cell, float level)
  {
    if (!cell.InBounds(map)) return;
    ApplyCoating(dirtLevels, map.cellIndices.CellToIndex(cell), cell, level);
  }

  public void SetDirtLevel(IntVec3 cell, float level, Color color)
  {
    if (!cell.InBounds(map)) return;
    int idx = map.cellIndices.CellToIndex(cell);
    bool colorChanged = dirtColors[idx] != color;
    dirtColors[idx] = color;
    ApplyCoating(dirtLevels, idx, cell, level);
    if (colorChanged) map.mapDrawer.MapMeshDirty(cell, MapMeshFlagDefOf.Roofs);
  }

  public void SetPollenLevel(IntVec3 cell, float level)
  {
    if (!cell.InBounds(map)) return;
    ApplyCoating(pollenLevels, map.cellIndices.CellToIndex(cell), cell, level);
  }

  public void SetSnowLevel(IntVec3 cell, float level)
  {
    if (!cell.InBounds(map)) return;
    int idx = map.cellIndices.CellToIndex(cell);
    SetSnowLevel(idx, cell, level);
  }

  public void SetSnowLevel(int idx, IntVec3 cell, float level)
  {
    ApplyCoating(snowLevels, idx, cell, level, SnowQuantum);
  }

  public void ClearAllCoating()
  {
    for (int i = 0; i < dirtLevels.Length; i++)
    {
      if (dirtLevels[i] > 0f || pollenLevels[i] > 0f || snowLevels[i] > 0f)
      {
        dirtLevels[i] = 0f;
        dirtColors[i] = Color.white;
        pollenLevels[i] = 0f;
        snowLevels[i] = 0f;
        NotifyCoatingChangedForced(map.cellIndices.IndexToCell(i));
      }
    }
  }

  public override void MapComponentTick()
  {
    base.MapComponentTick();

    if (!hasScanned)
    {
      ExecuteScan();
    }

    if (Find.TickManager.TicksGame % 250 != 0) return;
    if (!Stratum.Settings.enableSkylightCoating) return;

    AccumulateNaturalDirt();
    WashDirtWithRain();
  }

  private void AccumulateNaturalDirt()
  {
    int cellsToTick = Mathf.Max(1, map.Area / 2000);
    Season season = GenLocalDate.Season(map);
    bool isPollenSeason = (season == Season.Spring || season == Season.Summer);
    float rate = Stratum.Settings.skylightDirtAccumulationRate;
    float accumulation = 0.025f * rate;

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
          if (curPollen < 1f) ApplyCoating(pollenLevels, idx, cell, curPollen + accumulation);
        }
        else
        {
          float curDirt = dirtLevels[idx];
          if (curDirt < 1f) ApplyCoating(dirtLevels, idx, cell, curDirt + accumulation);
        }
      }
    }

    // Compounding dirt/pollen accumulation
    foreach (int idx in activeSkylightCells)
    {
      float curDirt = dirtLevels[idx];
      if (curDirt > 0.01f && curDirt < 1f && Rand.Value < curDirt * 0.05f * rate)
      {
        ApplyCoating(dirtLevels, idx, map.cellIndices.IndexToCell(idx), curDirt + accumulation);
      }

      float curPollen = pollenLevels[idx];
      if (curPollen > 0.01f && curPollen < 1f && Rand.Value < curPollen * 0.05f * rate)
      {
        ApplyCoating(pollenLevels, idx, map.cellIndices.IndexToCell(idx), curPollen + accumulation);
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
        if (dirtLevels[idx] <= 0.001f && pollenLevels[idx] <= 0.001f) continue;
        IntVec3 cell = map.cellIndices.IndexToCell(idx);
        if (dirtLevels[idx] > 0.001f) ApplyCoating(dirtLevels, idx, cell, dirtLevels[idx] - 0.05f);
        if (pollenLevels[idx] > 0.001f) ApplyCoating(pollenLevels, idx, cell, pollenLevels[idx] - 0.05f);
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
