using System.Collections.Generic;
using RimWorld;
using Verse;

namespace SolarWeb.Stratum.UI;

public class RoofOnFire : Alert_Critical
{
  private readonly List<Thing> roofFiresResult = new();

  public RoofOnFire()
  {
    defaultLabel = "Stratum_Alert_RoofOnFire".Translate();
    defaultExplanation = "Stratum_Alert_RoofOnFireDesc".Translate();
  }

  public override AlertReport GetReport()
  {
    if (Find.PlaySettings.showRoofOverlay)
    {
      return AlertReport.Inactive;
    }

    roofFiresResult.Clear();
    List<Map> maps = Find.Maps;
    for (int i = 0; i < maps.Count; i++)
    {
      Map map = maps[i];
      List<Thing> list = map.listerThings.ThingsOfDef(DefOf.ThingDefOf.RoofFire);
      for (int j = 0; j < list.Count; j++)
      {
        Thing thing = list[j];
        if (map.areaManager.Home[thing.Position] && !thing.Position.Fogged(map))
        {
          roofFiresResult.Add(thing);
        }
      }
    }

    return AlertReport.CulpritsAre(roofFiresResult);
  }

  protected override void OnClick()
  {
    base.OnClick();
    Find.PlaySettings.showRoofOverlay = true;
  }
}
