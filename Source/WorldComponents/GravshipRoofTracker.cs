using System.Collections.Generic;
using RimWorld.Planet;
using Verse;

namespace SolarWeb.Stratum.WorldComponents;

public class GravshipRoofTracker(World world) : WorldComponent(world)
{
  public class RoofCellData : IExposable
  {
    public ThingDef? stuff;
    public short hitPoints = -1;
    public UnityEngine.Color? glassTint;

    public void ExposeData()
    {
      Scribe_Defs.Look(ref stuff, "stuff");
      Scribe_Values.Look(ref hitPoints, "hp", (short)-1);
      Scribe_Values.Look(ref glassTint, "glassTint");
    }
  }

  public class RoofData : IExposable
  {
    public Dictionary<IntVec3, RoofCellData> roofs = [];
    public Dictionary<IntVec3, MapComponents.RoofConstructionTracker.ConstructionRecord> construction = [];

    public void ExposeData()
    {
      Scribe_Collections.Look(ref roofs, "roofs", LookMode.Value, LookMode.Deep);
      Scribe_Collections.Look(ref construction, "construction", LookMode.Value, LookMode.Deep);
    }
  }

  private Dictionary<int, RoofData> gravshipRoofs = [];

  public override void ExposeData()
  {
    base.ExposeData();
    Scribe_Collections.Look(ref gravshipRoofs, "gravshipRoofs", LookMode.Value, LookMode.Deep);
  }

  public void RegisterGravship(int id, Dictionary<IntVec3, RoofCellData> roofs, Dictionary<IntVec3, MapComponents.RoofConstructionTracker.ConstructionRecord> construction)
  {
    gravshipRoofs[id] = new RoofData { roofs = roofs, construction = construction };
  }

  public void UnregisterGravship(int id)
  {
    gravshipRoofs.Remove(id);
  }

  public bool TryGetRoofData(int id, out RoofData? data)
  {
    return gravshipRoofs.TryGetValue(id, out data);
  }
}
