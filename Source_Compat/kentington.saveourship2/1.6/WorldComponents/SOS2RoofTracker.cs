using System.Collections.Generic;
using RimWorld.Planet;
using Verse;

namespace SolarWeb.Stratum.SOS2.WorldComponents;

public class SOS2RoofTracker(World world) : WorldComponent(world)
{
  public class RoofCellData : IExposable
  {
    public RoofDef? roofDef;
    public ThingDef? stuff;
    public short hitPoints = -1;
    public UnityEngine.Color? glassTint;

    public void ExposeData()
    {
      Scribe_Defs.Look(ref roofDef, "roofDef");
      Scribe_Defs.Look(ref stuff, "stuff");
      Scribe_Values.Look(ref hitPoints, "hp", (short)-1);
      Scribe_Values.Look(ref glassTint, "glassTint");
    }
  }

  public class SOS2RoofData : IExposable
  {
    public Dictionary<IntVec3, RoofCellData> roofs = [];

    public void ExposeData()
    {
      Scribe_Collections.Look(ref roofs, "roofs", LookMode.Value, LookMode.Deep);
    }
  }

  public static void CaptureShipRoofs(Map sourceMap, HashSet<IntVec3> shipArea, int shipIndex, IntVec3 origin)
  {
    var roofTracker = Find.World.GetComponent<SOS2RoofTracker>();
    if (roofTracker == null)
      return;

    var roofs = new Dictionary<IntVec3, RoofCellData>();
    var integrityGrid = sourceMap.GetComponent<MapComponents.RoofIntegrityGrid>();

    foreach (var cell in shipArea)
    {
      var roofDef = sourceMap.roofGrid.RoofAt(cell);
      if (roofDef != null)
      {
        var cellData = new RoofCellData
        {
          roofDef = roofDef,
          stuff = integrityGrid?.GetStuff(cell),
          hitPoints = integrityGrid != null ? integrityGrid.GetHitPoints(cell) : (short)-1,
          glassTint = integrityGrid?.GetGlassTint(cell)
        };

        var localOffset = cell - origin;
        roofs[localOffset] = cellData;
      }
    }

    if (roofs.Count > 0)
    {
      roofTracker.RegisterSOS2Ship(shipIndex, roofs);
      StratumLog.Debug($"Captured {roofs.Count} roof cells for SOS2 ship {shipIndex} relative to origin {origin}");
    }
  }

  public static void RestoreShipRoofs(Map targetMap, int shipIndex, IntVec3 origin, byte rotNum = 0)
  {
    var roofTracker = Find.World.GetComponent<SOS2RoofTracker>();
    if (roofTracker == null)
      return;

    if (!roofTracker.TryGetSOS2RoofData(shipIndex, out var roofData) || roofData?.roofs == null)
      return;

    var integrityGrid = targetMap.GetComponent<MapComponents.RoofIntegrityGrid>();

    int restoredCount = 0;
    int rotb = 4 - rotNum;

    foreach (var kvp in roofData.roofs)
    {
      var localOffset = kvp.Key;
      IntVec3 transformedOffset;

      if (rotb == 2)
      {
        transformedOffset = new IntVec3(-localOffset.x, 0, -localOffset.z);
      }
      else if (rotb == 3)
      {
        transformedOffset = new IntVec3(-localOffset.z, 0, localOffset.x);
      }
      else
      {
        transformedOffset = localOffset;
      }

      var cell = origin + transformedOffset;
      var cellData = kvp.Value;

      if (targetMap.cellIndices.Contains(cell) && cellData.roofDef != null)
      {
        targetMap.roofGrid.SetRoof(cell, cellData.roofDef);
        if (integrityGrid != null)
        {
          integrityGrid.InitializeRoof(cell, cellData.roofDef, cellData.stuff, cellData.glassTint, cellData.hitPoints);
        }
        restoredCount++;
      }
    }

    if (restoredCount > 0)
    {
      StratumLog.Debug($"Restored {restoredCount} roof cells for SOS2 ship {shipIndex} relative to origin {origin} with rotNum {rotNum}");
    }

    roofTracker.UnregisterSOS2Ship(shipIndex);
  }

  private Dictionary<int, SOS2RoofData> sos2ShipRoofs = [];

  public override void ExposeData()
  {
    base.ExposeData();
    Scribe_Collections.Look(ref sos2ShipRoofs, "sos2ShipRoofs", LookMode.Value, LookMode.Deep);
  }

  public void RegisterSOS2Ship(int shipIndex, Dictionary<IntVec3, RoofCellData> roofs)
  {
    sos2ShipRoofs[shipIndex] = new SOS2RoofData { roofs = roofs };
    StratumLog.Debug($"Registered SOS2 ship {shipIndex} with {roofs.Count} roof cells");
  }

  public void UnregisterSOS2Ship(int shipIndex)
  {
    sos2ShipRoofs.Remove(shipIndex);
    StratumLog.Debug($"Unregistered SOS2 ship {shipIndex}");
  }

  public bool TryGetSOS2RoofData(int shipIndex, out SOS2RoofData? data)
  {
    return sos2ShipRoofs.TryGetValue(shipIndex, out data);
  }

  public void ClearAll()
  {
    sos2ShipRoofs.Clear();
    StratumLog.Debug("Cleared all SOS2 ship roof data");
  }
}