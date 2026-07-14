using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

using SolarWeb.Stratum.Stats;
using SolarWeb.Stratum.Hooks;

namespace SolarWeb.Stratum.MapComponents;

public class RoofVFXMapComponent : MapComponent
{
  private readonly List<int> transparentCells = [];
  private int[] cellToIndex = null!; // Maps cell index -> index in transparentCells list

  // Tracks how many transparent cells are in each 17x17 section
  private readonly Dictionary<IntVec2, int> sectionTransparentCounts = [];
  private readonly List<IntVec2> activeSections = [];

  public IReadOnlyList<int> TransparentCells => transparentCells;
  public IReadOnlyList<IntVec2> ActiveSections => activeSections;

  private const int TickInterval = 60; // Every 1s
  private const int MaxFlecksPerTick = 20;
  private const float MinSunlight = 0.3f;

  public RoofVFXMapComponent(Map map) : base(map) { }

  public override void FinalizeInit()
  {
    base.FinalizeInit();

    EnsureInitialized();

    var integrity = map.GetComponent<RoofIntegrityGrid>();
    if (integrity != null && !integrity.hasScanned)
    {
      integrity.ExecuteScan();
    }

    var registry = MapHookRegistry.Get(map);
    if (registry != null)
    {
      registry.Register<MapHookRegistry.RoofChangedHandler>(MapHookRegistry.HookId.RoofChanged, Notify_StratumRoofChanged);
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
    if (m == map) Notify_RoofChanged(c, newRoof);
  }

  internal void AddTransparentCellInternal(int cellIdx)
  {
    if (cellToIndex == null || cellIdx < 0 || cellIdx >= cellToIndex.Length) return;
    EnsureInitialized();
    if (cellToIndex[cellIdx] != -1) return;

    cellToIndex[cellIdx] = transparentCells.Count;
    transparentCells.Add(cellIdx);

    IntVec3 cell = map.cellIndices.IndexToCell(cellIdx);
    GetInfluencedSections(cell, tmpSections);
    foreach (IntVec2 sectionPos in tmpSections)
    {
      if (!sectionTransparentCounts.TryGetValue(sectionPos, out int count))
      {
        count = 0;
        activeSections.Add(sectionPos);
      }
      sectionTransparentCounts[sectionPos] = count + 1;
    }
  }

  private static readonly List<IntVec2> tmpSections = new(4);

  // A border cell's light reaches shared mesh verts in the neighboring section, so that
  // section must also regenerate as sky glow changes or a stale seam line appears.
  private void GetInfluencedSections(IntVec3 cell, List<IntVec2> sections)
  {
    sections.Clear();
    int maxX = map.Size.x - 1;
    int maxZ = map.Size.z - 1;
    for (int dx = -1; dx <= 1; dx++)
    {
      for (int dz = -1; dz <= 1; dz++)
      {
        int x = Mathf.Clamp(cell.x + dx, 0, maxX);
        int z = Mathf.Clamp(cell.z + dz, 0, maxZ);
        IntVec2 s = new(x / 17, z / 17);
        if (!sections.Contains(s)) sections.Add(s);
      }
    }
  }

  private void EnsureInitialized()
  {
    if (cellToIndex != null) return;

    int numGridCells = map.cellIndices.NumGridCells;
    cellToIndex = new int[numGridCells];
    for (int i = 0; i < numGridCells; i++) cellToIndex[i] = -1;

    sectionTransparentCounts.Clear();
    activeSections.Clear();
    transparentCells.Clear();
  }

  private void RemoveCellInternal(int cellIdx, int listIdx)
  {
    if (cellToIndex == null || cellIdx < 0 || cellIdx >= cellToIndex.Length) return;
    int lastIdx = transparentCells.Count - 1;
    if (listIdx != lastIdx)
    {
      int lastCell = transparentCells[lastIdx];
      transparentCells[listIdx] = lastCell;
      cellToIndex[lastCell] = listIdx;
    }

    transparentCells.RemoveAt(lastIdx);
    cellToIndex[cellIdx] = -1;

    IntVec3 cell = map.cellIndices.IndexToCell(cellIdx);
    GetInfluencedSections(cell, tmpSections);
    foreach (IntVec2 sectionPos in tmpSections)
    {
      if (sectionTransparentCounts.TryGetValue(sectionPos, out int count))
      {
        if (count <= 1)
        {
          sectionTransparentCounts.Remove(sectionPos);
          activeSections.Remove(sectionPos);
        }
        else
        {
          sectionTransparentCounts[sectionPos] = count - 1;
        }
      }
    }
  }

  public void Notify_RoofChanged(IntVec3 c, RoofDef? roof = null)
  {
    if (cellToIndex == null) return;

    int idx = map.cellIndices.CellToIndex(c);
    if (roof == null) roof = map.roofGrid.RoofAt(idx);
    bool isTransparent = roof != null && RoofStatCache.GetTransparency(roof) > 0f;

    int listIdx = cellToIndex[idx];
    bool inList = listIdx != -1;

    if (isTransparent && !inList)
    {
      AddTransparentCellInternal(idx);
    }
    else if (!isTransparent && inList)
    {
      RemoveCellInternal(idx, listIdx);
    }
  }

  public override void MapComponentTick()
  {
    if (map.skyManager == null) return;
    float curSkyGlow = map.skyManager.CurSkyGlow;

    // No sky-glow-based mesh dirtying: the lighting overlay bakes nothing time-of-day
    // dependent (the sky term animates via the material color), so regenerating sections as
    // the sky changes is wasted work — and sections regenerating at different times produced
    // stale seams and mismatched section brightness.

    if (Find.TickManager.TicksGame % TickInterval == 0 && transparentCells.Count > 0)
    {
      if (curSkyGlow >= MinSunlight)
      {
        SpawnFlecks(curSkyGlow);
      }
    }
  }

  private void SpawnFlecks(float sunGlow)
  {
    if (map.flecks == null || SolarWeb.Stratum.DefOf.FleckDefOf.Sunbeam == null || RimWorld.FleckDefOf.HeatGlow == null) return;

    try
    {
      // Lifetime is 10s (600 ticks). 
      // Target sparse distribution: approx 1 beam per 50 cells every 10s.
      float spawnChancePerCell = 0.002f * sunGlow;
      int spawnedThisTick = 0;

      int startIndex = Rand.Range(0, transparentCells.Count);

      for (int i = 0; i < transparentCells.Count; i++)
      {
        if (spawnedThisTick >= MaxFlecksPerTick) break;

        int listIndex = (startIndex + i) % transparentCells.Count;
        if (Rand.Value > spawnChancePerCell) continue;

        int cellIdx = transparentCells[listIndex];
        IntVec3 pos = map.cellIndices.IndexToCell(cellIdx);
        var roof = map.roofGrid.RoofAt(cellIdx);
        if (roof == null) continue;

        float transparency = RoofStatCache.GetTransparency(roof);
        if (transparency <= 0f) continue;

        Color roofColor = RoofStatCache.GetGlassTint(roof, map, pos);
        Vector3 center = pos.ToVector3Shifted();
        center.y += 0.1f;

        float effectivePower = sunGlow * Mathf.Sqrt(transparency);

        FleckCreationData data = FleckMaker.GetDataStatic(center + new Vector3(Rand.Range(-0.4f, 0.4f), 0, Rand.Range(-0.4f, 0.4f)), map, SolarWeb.Stratum.DefOf.FleckDefOf.Sunbeam);
        data.scale = Rand.Range(0.8f, 1.2f);
        Color beamColor = roofColor;
        beamColor.a = Mathf.Lerp(0.4f, 1.0f, effectivePower);
        data.instanceColor = beamColor;
        map.flecks.CreateFleck(data);

        FleckCreationData glowData = FleckMaker.GetDataStatic(center, map, RimWorld.FleckDefOf.HeatGlow);
        glowData.scale = Rand.Range(1.5f, 2.0f);
        Color glowColor = roofColor;
        glowColor.a = Mathf.Lerp(0.2f, 0.6f, effectivePower);
        glowData.instanceColor = glowColor;
        map.flecks.CreateFleck(glowData);

        spawnedThisTick++;
      }
    }
    catch (System.Exception ex)
    {
      StratumLog.Error($"Error in RoofVFXMapComponent.SpawnFlecks: {ex}");
    }
  }
}
