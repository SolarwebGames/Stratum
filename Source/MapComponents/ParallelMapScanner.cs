using System.Collections.Generic;
using Verse;
using System.Threading.Tasks;

using SolarWeb.Stratum.Stats;

namespace SolarWeb.Stratum.MapComponents;

public static class ParallelMapScanner
{
  private class LocalState
  {
    public readonly List<int> solar = new();
    public readonly List<int> vfx = new();
    public readonly Map map;
    public readonly RoofGrid roofGrid;
    public readonly RoofIntegrityGrid integrity;
    public readonly SolarRoofMapComponent solarComponent;
    public readonly RoofVFXMapComponent vfxComponent;
    public readonly List<int> finalSolar;
    public readonly List<int> finalVfx;
    public readonly object sharedLock;

    public LocalState(Map map, RoofGrid roofGrid, RoofIntegrityGrid integrity, SolarRoofMapComponent solarComponent, RoofVFXMapComponent vfxComponent, List<int> finalSolar, List<int> finalVfx, object sharedLock)
    {
      this.map = map;
      this.roofGrid = roofGrid;
      this.integrity = integrity;
      this.solarComponent = solarComponent;
      this.vfxComponent = vfxComponent;
      this.finalSolar = finalSolar;
      this.finalVfx = finalVfx;
      this.sharedLock = sharedLock;
    }
  }

  private static readonly object GlobalScanLock = new();

  public static void ExecuteScan(Map map, bool force = false)
  {
    var integrity = map.GetComponent<RoofIntegrityGrid>();
    if (integrity == null) return;

    // Use a global lock to prevent concurrent scans on the same map
    // This is safer than just locking on integrity for the flag check
    lock (GlobalScanLock)
    {
      if (integrity.hasScanned && !force) return;
      integrity.hasScanned = true;
    }

    var solar = map.GetComponent<SolarRoofMapComponent>();
    var vfx = map.GetComponent<RoofVFXMapComponent>();

    int numCells = map.cellIndices.NumGridCells;
    var roofGrid = map.roofGrid;
    var finalSolar = new List<int>();
    var finalVfx = new List<int>();
    var sharedLock = new object();

    Parallel.For(0, numCells,
      () => new LocalState(map, roofGrid, integrity, solar, vfx, finalSolar, finalVfx, sharedLock),
      LoopBody,
      LoopFinally
    );

    if (solar != null)
    {
      foreach (int idx in finalSolar) solar.AddSolarCellInternal(idx);
    }

    if (vfx != null)
    {
      foreach (int idx in finalVfx) vfx.AddTransparentCellInternal(idx);
    }
  }

  static LocalState LoopBody(int i, ParallelLoopState loopState, LocalState local)
  {
    var roofGrid = local.roofGrid;
    var integrity = local.integrity;
    var solar = local.solarComponent;
    var vfx = local.vfxComponent;
    var roof = roofGrid.RoofAt(i);
    if (roof == null) return local;

    if (RoofStatCache.IsCustomRoof(roof))
    {
      if (integrity.HitPointsArray[i] == 0)
      {
        integrity.HitPointsArray[i] = (short)RoofStatCache.GetMaxHitPoints(roof);
      }
    }

    if (solar != null && RoofStatCache.GetSolarEfficiency(roof) > 0f)
    {
      local.solar.Add(i);
    }

    if (vfx != null && RoofStatCache.GetTransparency(roof) > 0f)
    {
      local.vfx.Add(i);
    }

    return local;
  }

  static void LoopFinally(LocalState local)
  {
    lock (local.sharedLock)
    {
      if (local.solar.Count > 0)
      {
        local.finalSolar.AddRange(local.solar);
      }
      if (local.vfx.Count > 0)
      {
        local.finalVfx.AddRange(local.vfx);
      }
    }
  }
}
