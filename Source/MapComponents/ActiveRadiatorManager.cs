using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

using SolarWeb.Stratum.DefModExtensions;

namespace SolarWeb.Stratum.MapComponents;

public class ActiveRadiatorManager(Map map) : MapComponent(map)
{
  private bool dirty = true;
  private readonly HashSet<int> radiatorCells = [];

  private readonly Dictionary<PowerNet, float> powerDraws = [];

  private readonly Dictionary<Room, List<int>> roomGroups = [];
  private readonly List<List<int>> listPool = [];
  private int listPoolIndex = 0;

  private readonly Dictionary<PowerNet, float> netRequests = [];
  private readonly Dictionary<int, PowerNet> cellToNet = [];
  private readonly HashSet<PowerNet> poweredNets = [];

  private readonly Dictionary<RoofDef, ActiveRadiatorRoofExtension> extCache = [];

  private readonly Dictionary<Room, (int total, int powered, bool cooling, float powerDraw)> roomStats = [];

  private readonly List<List<int>> roofClusters = [];

  public override void FinalizeInit()
  {
    base.FinalizeInit();
    dirty = true;
    Utilities.StratumHooks.OnRoofChanged += Notify_RoofChanged;
    Utilities.StratumHooks.OnCalculateEnergyGainRate += HandleEnergyGainRate;

    foreach (var cell in map.AllCells)
    {
      var roof = map.roofGrid.RoofAt(cell);
      if (roof != null && roof.HasModExtension<ActiveRadiatorRoofExtension>())
      {
        radiatorCells.Add(map.cellIndices.CellToIndex(cell));
      }
    }
  }

  public override void MapRemoved()
  {
    base.MapRemoved();
    radiatorCells.Clear();
    powerDraws.Clear();
    Utilities.StratumHooks.OnRoofChanged -= Notify_RoofChanged;
    Utilities.StratumHooks.OnCalculateEnergyGainRate -= HandleEnergyGainRate;
  }

  private void HandleEnergyGainRate(PowerNet net, ref float energyGainRate)
  {
    if (net.Map != map) return;
    float powerDraw = GetPowerDrawFor(net);
    if (powerDraw > 0f)
    {
      energyGainRate -= powerDraw / 60000f;
    }
  }

  private void Notify_RoofChanged(Map m, IntVec3 c, RoofDef? oldRoof, RoofDef? newRoof)
  {
    if (m != map) return;
    int idx = map.cellIndices.CellToIndex(c);

    if (newRoof != null && newRoof.HasModExtension<ActiveRadiatorRoofExtension>())
    {
      radiatorCells.Add(idx);
    }
    else
    {
      radiatorCells.Remove(idx);
    }
    dirty = true;
  }

  public float GetPowerDrawFor(PowerNet net)
  {
    if (powerDraws.TryGetValue(net, out float draw))
      return draw;
    return 0f;
  }

  public bool TryGetRoomStats(Room room, out int totalTiles, out int poweredTiles, out bool cooling, out float powerDraw)
  {
    if (roomStats.TryGetValue(room, out var stats))
    {
      totalTiles = stats.total;
      poweredTiles = stats.powered;
      cooling = stats.cooling;
      powerDraw = stats.powerDraw;
      return true;
    }
    totalTiles = 0;
    poweredTiles = 0;
    cooling = false;
    powerDraw = 0f;
    return false;
  }

  public override void MapComponentTick()
  {
    if (Find.TickManager.TicksGame % 250 == 0)
    {
      if (dirty)
      {
        RebuildClusters();
        dirty = false;
      }
      TickRare();
    }
  }

  private void RebuildClusters()
  {
    foreach (var cluster in roofClusters)
      cluster.Clear();

    var unvisited = new HashSet<int>(radiatorCells);
    var queue = new Queue<int>();

    int clusterIndex = 0;

    while (unvisited.Count > 0)
    {
      var e = unvisited.GetEnumerator();
      e.MoveNext();
      int startIdx = e.Current;
      e.Dispose();

      if (clusterIndex >= roofClusters.Count)
        roofClusters.Add([]);

      var cluster = roofClusters[clusterIndex++];

      queue.Enqueue(startIdx);
      unvisited.Remove(startIdx);

      while (queue.Count > 0)
      {
        int curr = queue.Dequeue();
        cluster.Add(curr);

        IntVec3 cell = map.cellIndices.IndexToCell(curr);
        for (int i = 0; i < 4; i++)
        {
          IntVec3 adj = cell + GenAdj.CardinalDirections[i];
          if (adj.InBounds(map))
          {
            int adjIdx = map.cellIndices.CellToIndex(adj);
            if (unvisited.Contains(adjIdx))
            {
              unvisited.Remove(adjIdx);
              queue.Enqueue(adjIdx);
            }
          }
        }
      }
    }

    if (roofClusters.Count > clusterIndex)
    {
      roofClusters.RemoveRange(clusterIndex, roofClusters.Count - clusterIndex);
    }
  }

  private ActiveRadiatorRoofExtension? GetExtension(RoofDef roof)
  {
    if (roof == null) return null;
    if (!extCache.TryGetValue(roof, out var ext))
    {
      ext = roof.GetModExtension<ActiveRadiatorRoofExtension>();
      extCache[roof] = ext;
    }
    return ext;
  }

  private List<int> GetList()
  {
    if (listPoolIndex >= listPool.Count)
    {
      listPool.Add([]);
    }
    var list = listPool[listPoolIndex++];
    list.Clear();
    return list;
  }

  private void TickRare()
  {
    powerDraws.Clear();
    if (radiatorCells.Count == 0) return;

    roomGroups.Clear();
    listPoolIndex = 0;
    netRequests.Clear();
    cellToNet.Clear();
    poweredNets.Clear();
    roomStats.Clear();

    var thermostatTracker = map.GetComponent<ThermostatTracker>();
    var availableNets = new List<PowerNet>();

    foreach (var cluster in roofClusters)
    {
      if (cluster.Count == 0) continue;

      availableNets.Clear();
      Room? primaryRoom = null;

      foreach (int idx in cluster)
      {
        IntVec3 cell = map.cellIndices.IndexToCell(idx);
        if (primaryRoom == null)
        {
          Room r = cell.GetRoom(map);
          if (r != null && !r.TouchesMapEdge && !r.UsesOutdoorTemperature)
            primaryRoom = r;
        }

        PowerNet net = map.powerNetGrid.TransmittedPowerNetAt(cell);
        if (net != null && !availableNets.Contains(net))
        {
          availableNets.Add(net);
        }
      }

      if (availableNets.Count > 0)
      {
        int offset = 0;
        if (primaryRoom != null && thermostatTracker != null)
        {
          offset = thermostatTracker.GetNetworkOffsetForRoom(primaryRoom);
        }

        PowerNet chosenNet = availableNets[offset % availableNets.Count];

        foreach (int idx in cluster)
        {
          cellToNet[idx] = chosenNet;
        }
      }
    }

    foreach (var idx in radiatorCells)
    {
      IntVec3 cell = map.cellIndices.IndexToCell(idx);
      Room room = cell.GetRoom(map);
      if (room == null || room.TouchesMapEdge || room.UsesOutdoorTemperature) continue;

      if (!roomGroups.TryGetValue(room, out var list))
      {
        list = GetList();
        roomGroups[room] = list;
      }
      list.Add(idx);
    }

    float outdoorTemp = map.mapTemperature.OutdoorTemp;

    foreach (var kvp in roomGroups)
    {
      Room room = kvp.Key;
      var cells = kvp.Value;
      if (cells.Count == 0) continue;

      IntVec3 firstCell = map.cellIndices.IndexToCell(cells[0]);
      RoofDef roof = map.roofGrid.RoofAt(firstCell);
      var ext = GetExtension(roof);
      if (ext == null) continue;

      float targetTemp = ext.targetTemperature;
      if (thermostatTracker != null && thermostatTracker.TryGetLowestTargetTemperature(room, out float thermostatTemp))
        targetTemp = thermostatTemp;

      bool needsCooling = room.Temperature > targetTemp;

      foreach (var idx in cells)
      {
        if (cellToNet.TryGetValue(idx, out PowerNet net))
        {
          float draw = needsCooling ? ext.powerDrawActive : ext.powerDrawStandby;
          if (!netRequests.ContainsKey(net)) netRequests[net] = 0f;
          netRequests[net] += draw;
        }
      }
    }

    foreach (var kvp in netRequests)
    {
      PowerNet net = kvp.Key;
      float requested = kvp.Value;

      if (net.CurrentStoredEnergy() > 0f || (net.CurrentEnergyGainRate() * 60000f) >= requested)
      {
        poweredNets.Add(net);
        powerDraws[net] = requested;
      }
    }

    foreach (var kvp in roomGroups)
    {
      Room room = kvp.Key;
      var cells = kvp.Value;
      if (cells.Count == 0) continue;

      IntVec3 firstCell = map.cellIndices.IndexToCell(cells[0]);
      RoofDef roof = map.roofGrid.RoofAt(firstCell);
      var ext = GetExtension(roof);
      if (ext == null) continue;

      float targetTemp = ext.targetTemperature;
      if (thermostatTracker != null && thermostatTracker.TryGetLowestTargetTemperature(room, out float thermostatTemp))
        targetTemp = thermostatTemp;

      bool needsCooling = room.Temperature > targetTemp;

      float activeTiles = 0f;
      float powerDraw = 0f;
      foreach (var idx in cells)
      {
        if (cellToNet.TryGetValue(idx, out PowerNet net) && poweredNets.Contains(net))
        {
          activeTiles++;
          powerDraw += needsCooling ? ext.powerDrawActive : ext.powerDrawStandby;
        }
      }

      roomStats[room] = (cells.Count, (int)activeTiles, needsCooling, powerDraw);

      if (!needsCooling) continue;

      float roomCoolingEfficiency = 1f;
      if (outdoorTemp > room.Temperature)
      {
        float tempDiff = outdoorTemp - room.Temperature;
        roomCoolingEfficiency = Mathf.Max(0.1f, 1f - (tempDiff / 80f));
      }

      if (activeTiles > 0)
      {
        // 4.1666665f is the 250-tick multiplier for energyPerSecond used by vanilla CompTempControl
        float energyPerSecond = ext.coolingCapacity * activeTiles * roomCoolingEfficiency;
        float actualHeat = -energyPerSecond * 4.1666665f;

        float tempChange = actualHeat / (room.CellCount * 1.2f);
        if (room.Temperature + tempChange < targetTemp)
        {
          actualHeat = (targetTemp - room.Temperature) * (room.CellCount * 1.2f);
        }

        GenTemperature.PushHeat(firstCell, map, actualHeat);
      }
    }
  }
}
