using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

using SolarWeb.Stratum.DefModExtensions;

namespace SolarWeb.Stratum.Utilities;

public static class RoofStuffUtility
{
  public static int GetAccessibleStuffCount(ThingDef stuff, Map map)
  {
    if (map == null) return 0;
    int count = 0;
    foreach (var t in map.listerThings.ThingsOfDef(stuff))
    {
      if (!t.IsForbidden(Faction.OfPlayer))
      {
        count += t.stackCount;
      }
    }
    return count;
  }

  public static ThingDef? GetCheapestAvailableStuff(BuildableDef placingDef, Map map)
  {
    if (placingDef is ThingDef { MadeFromStuff: true } thingDef && map != null)
    {
      ThingDef? cheapestStuff = null;
      float minVal = float.MaxValue;

      foreach (var stuff in GenStuff.AllowedStuffsFor(thingDef))
      {
        if (GetAccessibleStuffCount(stuff, map) >= thingDef.CostStuffCount)
        {
          float val = stuff.BaseMarketValue;
          if (val < minVal)
          {
            minVal = val;
            cheapestStuff = stuff;
          }
        }
      }

      if (cheapestStuff != null) return cheapestStuff;

      return GenStuff.DefaultStuffFor(thingDef);
    }
    return null;
  }
}
