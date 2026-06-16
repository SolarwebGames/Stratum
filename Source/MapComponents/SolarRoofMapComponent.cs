using System.Collections.Generic;
using System.Threading.Tasks;
using RimWorld;
using Verse;

using SolarWeb.Stratum.Stats;

namespace SolarWeb.Stratum.MapComponents;

public class SolarRoofMapComponent : MapComponent
{
  private bool dirty = true;
  private bool isCalculating = false;
  private readonly object lockObject = new();

  private readonly HashSet<int> solarRoofCells = [];
  private List<SolarNetwork> networks = [];
  private Dictionary<PowerNet, float> netToSolarPower = [];
  private Dictionary<int, SolarPowerStats> cellToPower = [];

  public struct SolarPowerStats
  {
    public float currentPower;
    public float maxPower;
  }

  public SolarRoofMapComponent(Map map) : base(map) { }

  public override void FinalizeInit()
  {
    base.FinalizeInit();
    dirty = true;
    Utilities.StratumHooks.OnRoofChanged += Notify_StratumRoofChanged;
  }

  internal void AddSolarCellInternal(int index)
  {
    solarRoofCells.Add(index);
  }

  public override void MapRemoved()
  {
    base.MapRemoved();
    solarRoofCells.Clear();
    lock (lockObject)
    {
      networks.Clear();
      netToSolarPower.Clear();
      cellToPower.Clear();
    }
    Utilities.StratumHooks.OnRoofChanged -= Notify_StratumRoofChanged;
  }

  public override void MapComponentTick()
  {
    if (dirty)
    {
      RebuildNetworks();
      dirty = false;
    }

    if (Find.TickManager.TicksGame % 250 == 0)
    {
      UpdatePowerNets();
    }
  }

  private void Notify_StratumRoofChanged(Map m, IntVec3 c, RoofDef? oldRoof, RoofDef? newRoof)
  {
    if (m == map) Notify_RoofChanged(c, newRoof);
  }

  public void Notify_RoofChanged(IntVec3 c, RoofDef? roof = null)
  {
    int idx = map.cellIndices.CellToIndex(c);
    if (roof == null) roof = map.roofGrid.RoofAt(idx);

    if (roof != null && RoofStatCache.GetSolarEfficiency(roof) > 0f)
    {
      solarRoofCells.Add(idx);
    }
    else
    {
      solarRoofCells.Remove(idx);
    }

    dirty = true;
  }

  public float GetAdditionalPowerFor(PowerNet net)
  {
    lock (lockObject)
    {
      if (netToSolarPower.TryGetValue(net, out float power))
      {
        return power;
      }
    }
    return 0f;
  }

  public bool TryGetSolarNetworkPower(IntVec3 cell, out SolarPowerStats cellStats, out SolarPowerStats netStats)
  {
    int idx = map.cellIndices.CellToIndex(cell);
    lock (lockObject)
    {
      if (cellToPower.TryGetValue(idx, out cellStats))
      {
        foreach (var net in networks)
        {
          foreach (var ci in net.cells)
          {
            if (ci.cellIdx == idx)
            {
              netStats = new SolarPowerStats { currentPower = net.currentPower, maxPower = net.maxPower };
              return true;
            }
          }
        }
      }
    }

    cellStats = default;
    netStats = default;
    return false;
  }

  private void RebuildNetworks()
  {
    var newNetworks = new List<SolarNetwork>();
    if (solarRoofCells.Count == 0)
    {
      networks = newNetworks;
      return;
    }

    HashSet<int> visited = [];
    foreach (int i in solarRoofCells)
    {
      if (visited.Contains(i)) continue;

      var net = new SolarNetwork();
      FloodFill(i, net, visited);
      if (net.cells.Count > 0)
      {
        newNetworks.Add(net);
      }
    }

    // Atomically swap the list so the background thread's snapshot remains stable
    networks = newNetworks;
  }

  private void FloodFill(int startCell, SolarNetwork net, HashSet<int> visited)
  {
    Queue<int> queue = new();
    queue.Enqueue(startCell);
    visited.Add(startCell);

    while (queue.Count > 0)
    {
      int currIdx = queue.Dequeue();
      var roof = map.roofGrid.RoofAt(currIdx);
      if (roof == null) continue;

      float efficiency = RoofStatCache.GetSolarEfficiency(roof);
      if (efficiency <= 0f) continue;

      net.cells.Add(new SolarCellInfo
      {
        cellIdx = currIdx,
        baseEfficiency = efficiency,
        maxHP = RoofStatCache.GetMaxHitPoints(roof)
      });

      IntVec3 currPos = map.cellIndices.IndexToCell(currIdx);
      foreach (var dir in GenAdj.CardinalDirections)
      {
        IntVec3 nextPos = currPos + dir;
        if (nextPos.InBounds(map))
        {
          int nextIdx = map.cellIndices.CellToIndex(nextPos);
          if (!visited.Contains(nextIdx) && solarRoofCells.Contains(nextIdx))
          {
            visited.Add(nextIdx);
            queue.Enqueue(nextIdx);
          }
        }
      }
    }
  }

  private void UpdatePowerNets()
  {
    if (isCalculating) return;

    var snapshot = networks;
    if (snapshot.Count == 0)
    {
      lock (lockObject)
      {
        netToSolarPower.Clear();
        cellToPower.Clear();
      }
      return;
    }

    if (map.gameConditionManager.ElectricityDisabled(map))
    {
      lock (lockObject)
      {
        netToSolarPower.Clear();
        cellToPower.Clear();
      }
      return;
    }

    float skyGlow = map.skyManager.CurSkyGlow;
    var integrityGrid = map.GetComponent<RoofIntegrityGrid>();
    if (integrityGrid == null) return;

    isCalculating = true;

    // Collect PowerNets on the main thread safely
    var cellToNetMap = new Dictionary<int, PowerNet>();
    foreach (var net in snapshot)
    {
      foreach (var cellInfo in net.cells)
      {
        IntVec3 pos = map.cellIndices.IndexToCell(cellInfo.cellIdx);
        var pNet = map.powerNetGrid.TransmittedPowerNetAt(pos);
        if (pNet != null)
        {
          cellToNetMap[cellInfo.cellIdx] = pNet;
        }
      }
    }

    Task.Run(() =>
    {
      try
      {
        var netResults = new Dictionary<PowerNet, float>();
        var cellResults = new Dictionary<int, SolarPowerStats>();
        const float BasePowerPerCell = 100f;

        foreach (var net in snapshot)
        {
          float netCurrentPower = 0f;
          float netMaxPower = 0f;
          var touchedNets = new HashSet<PowerNet>();

          foreach (var cellInfo in net.cells)
          {
            if (cellToNetMap.TryGetValue(cellInfo.cellIdx, out var pNet))
            {
              touchedNets.Add(pNet);
            }

            float curEff = cellInfo.baseEfficiency;
            if (cellInfo.maxHP > 0)
            {
              IntVec3 pos = map.cellIndices.IndexToCell(cellInfo.cellIdx);
              short hp = integrityGrid.GetHitPoints(pos);
              curEff *= (float)hp / cellInfo.maxHP;
            }

            float cellCur = curEff * skyGlow * BasePowerPerCell;
            float cellMax = cellInfo.baseEfficiency * BasePowerPerCell;

            cellResults[cellInfo.cellIdx] = new SolarPowerStats { currentPower = cellCur, maxPower = cellMax };

            netCurrentPower += cellCur;
            netMaxPower += cellMax;
          }

          net.currentPower = netCurrentPower;
          net.maxPower = netMaxPower;

          if (touchedNets.Count > 0)
          {
            float powerPerNet = netCurrentPower / touchedNets.Count;
            foreach (var pNet in touchedNets)
            {
              if (!netResults.ContainsKey(pNet)) netResults[pNet] = 0f;
              netResults[pNet] += powerPerNet;
            }
          }
        }

        lock (lockObject)
        {
          netToSolarPower = netResults;
          cellToPower = cellResults;
        }
      }
      finally
      {
        isCalculating = false;
      }
    });
  }

  private struct SolarCellInfo
  {
    public int cellIdx;
    public float baseEfficiency;
    public int maxHP;
  }

  private class SolarNetwork
  {
    public List<SolarCellInfo> cells = [];
    public float currentPower;
    public float maxPower;
  }
}
