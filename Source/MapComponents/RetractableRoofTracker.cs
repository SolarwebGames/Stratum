using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace SolarWeb.Stratum.MapComponents;

public class RetractableRoofTracker(Map map) : MapComponent(map)
{
  private List<int> openCellIndices = [];
  private List<RoofDef> originalRoofDefs = [];
  private List<ThingDef?> stuffDefs = [];
  private List<Color> glassTints = [];
  private List<short> hitPoints = [];

  public override void ExposeData()
  {
    base.ExposeData();
    Scribe_Collections.Look(ref openCellIndices, "openCellIndices", LookMode.Value);
    Scribe_Collections.Look(ref originalRoofDefs, "originalRoofDefs", LookMode.Def);
    Scribe_Collections.Look(ref stuffDefs, "stuffDefs", LookMode.Def);
    Scribe_Collections.Look(ref glassTints, "glassTints", LookMode.Value);
    Scribe_Collections.Look(ref hitPoints, "hitPoints", LookMode.Value);

    if (Scribe.mode == LoadSaveMode.PostLoadInit)
    {
      openCellIndices ??= [];
      originalRoofDefs ??= [];
      stuffDefs ??= [];
      glassTints ??= [];
      hitPoints ??= [];
    }
  }

  public void SaveOpenRoof(int cellIndex, RoofDef roofDef, ThingDef? stuff, Color? tint, short hp)
  {
    openCellIndices.Add(cellIndex);
    originalRoofDefs.Add(roofDef);
    stuffDefs.Add(stuff);
    glassTints.Add(tint ?? Color.white);
    hitPoints.Add(hp);
  }

  public bool PopOpenRoof(int cellIndex, out RoofDef roofDef, out ThingDef? stuff, out Color? tint, out short hp)
  {
    int i = openCellIndices.IndexOf(cellIndex);
    if (i >= 0)
    {
      roofDef = originalRoofDefs[i];
      stuff = stuffDefs[i];
      tint = glassTints[i];
      hp = hitPoints[i];

      openCellIndices.RemoveAt(i);
      originalRoofDefs.RemoveAt(i);
      stuffDefs.RemoveAt(i);
      glassTints.RemoveAt(i);
      hitPoints.RemoveAt(i);
      return true;
    }
    roofDef = null!;
    stuff = null;
    tint = null;
    hp = 0;
    return false;
  }

  public bool PeekOpenRoof(int cellIndex, out RoofDef roofDef, out ThingDef? stuff, out Color? tint, out short hp)
  {
    int i = openCellIndices.IndexOf(cellIndex);
    if (i >= 0)
    {
      roofDef = originalRoofDefs[i];
      stuff = stuffDefs[i];
      tint = glassTints[i];
      hp = hitPoints[i];
      return true;
    }

    roofDef = null!;
    stuff = null;
    tint = null;
    hp = 0;
    return false;
  }

  public bool IsRetracted(int cellIndex)
  {
    return openCellIndices.Contains(cellIndex);
  }

  public IEnumerable<int> GetRetractedCells()
  {
    return openCellIndices;
  }
}
