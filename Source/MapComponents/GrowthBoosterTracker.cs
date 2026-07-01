using System.Collections.Generic;
using Verse;

namespace SolarWeb.Stratum.MapComponents;

public class GrowthBoosterTracker(Map map) : MapComponent(map)
{
  public readonly HashSet<ThingComps.GrowthBooster> boosters = [];

  public void Register(ThingComps.GrowthBooster b)
  {
    boosters.Add(b);
  }

  public void Deregister(ThingComps.GrowthBooster b)
  {
    boosters.Remove(b);
  }
}
