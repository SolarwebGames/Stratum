using System.Collections.Generic;
using RimWorld;
using Verse;

using SolarWeb.Stratum.DefModExtensions;
using SolarWeb.Stratum.Things;

namespace SolarWeb.Stratum.MapComponents;

public class RoofConstructionTracker(Map map) : MapComponent(map)
{
  public class ConstructionRecord : IExposable
  {
    public RoofDef roofDef = null!;
    public ThingDef? stuffDef;
    public float workDone;
    public float workTotal;
    public UnityEngine.Color? glassTint;

    public void ExposeData()
    {
      Scribe_Defs.Look(ref roofDef, "roofDef");
      Scribe_Defs.Look(ref stuffDef, "stuffDef");
      Scribe_Values.Look(ref workDone, "workDone");
      Scribe_Values.Look(ref workTotal, "workTotal");
      Scribe_Values.Look(ref glassTint, "glassTint");
    }
  }

  private Dictionary<IntVec3, ConstructionRecord> records = [];

  public override void ExposeData()
  {
    base.ExposeData();
    Scribe_Collections.Look(ref records, "records", LookMode.Value, LookMode.Deep);
    records ??= [];
  }

  public void AddRecord(IntVec3 cell, RoofDef def, float total, ThingDef? stuff = null, UnityEngine.Color? glassTint = null)
  {
    records[cell] = new ConstructionRecord { roofDef = def, workTotal = total, stuffDef = stuff, glassTint = glassTint };
  }

  public void RebuildRoof(IntVec3 cell, RoofDef roofDef, BuildableRoofExtension ext, ThingDef? stuff = null, UnityEngine.Color? glassTint = null)
  {
    RemoveRecord(cell);

    float workToBuild = 1000f;
    var bDef = ext.buildableDef;
    if (bDef != null)
    {
      workToBuild = bDef.statBases.GetStatValueFromList(StatDefOf.WorkToBuild, 1000f);
    }

    AddRecord(cell, roofDef, workToBuild, stuff, glassTint);
    var frame = (RoofFrame)ThingMaker.MakeThing(DefOf.ThingDefOf.RoofFrame);
    frame.targetRoofDef = roofDef;
    frame.targetRoofStuff = stuff;
    frame.glassTint = glassTint;
    frame.SetFaction(Faction.OfPlayer);
    GenSpawn.Spawn(frame, cell, map);
  }

  public void RemoveRecord(IntVec3 cell, DestroyMode mode = DestroyMode.Vanish)
  {
    if (records.Remove(cell))
    {
      if (map.thingGrid != null)
      {
        var frame = map.thingGrid.ThingAt<RoofFrame>(cell);
        if (frame != null && !frame.Destroyed) frame.Destroy(mode);
      }
      if (map.areaManager != null)
      {
        map.areaManager.NoRoof[cell] = false;
        map.areaManager.NoRoof.MarkForDraw();
        map.areaManager.BuildRoof[cell] = false;
        map.areaManager.BuildRoof.MarkForDraw();
      }
    }
  }

  public void RemoveRecordInternal(IntVec3 cell)
  {
    records.Remove(cell);
  }

  public void RestoreRecord(IntVec3 cell, ConstructionRecord record)
  {
    records[cell] = record;
  }

  public bool TryGetRecord(IntVec3 cell, out ConstructionRecord record)
  {
    return records.TryGetValue(cell, out record);
  }

  public void CompleteConstruction(IntVec3 cell)
  {
    if (records.TryGetValue(cell, out var rec))
    {
      if (map.roofGrid != null)
      {
        map.roofGrid.SetRoof(cell, rec.roofDef);
      }
      
      if (Find.PlaySettings != null && Find.PlaySettings.autoHomeArea && map.areaManager.Home != null)
      {
        map.areaManager.Home[cell] = true;
      }

      map.GetComponent<RoofIntegrityGrid>()?.InitializeRoof(cell, rec.roofDef, rec.stuffDef, rec.glassTint);

      RemoveRecord(cell);
    }
  }
}
