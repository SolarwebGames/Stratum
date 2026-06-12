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

  public static ThingDef? GetCheapestAvailableStuff(BuildableDef placingDef, BuildableRoofExtension ext, Map map)
  {
    if (placingDef is ThingDef { MadeFromStuff: true } thingDef && map != null)
    {
      ThingDef? cheapestStuff = null;
      float minVal = float.MaxValue;

      foreach (var stuff in GenStuff.AllowedStuffsFor(thingDef))
      {
        if (ext.allowedStuff != null && !ext.allowedStuff.Contains(stuff)) continue;

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

      var defaultStuff = GenStuff.DefaultStuffFor(thingDef);
      if (defaultStuff != null && ext.allowedStuff != null && !ext.allowedStuff.Contains(defaultStuff))
      {
        return ext.allowedStuff.FirstOrDefault();
      }

      return defaultStuff;
    }
    return null;
  }

  public static void GenerateStuffSelectionMenu(ThingDef thingDef, BuildableRoofExtension ext, Map map, bool godMode, System.Action<ThingDef> onSelected)
  {
    var list = new List<FloatMenuOption>();
    foreach (var item in GenStuff.AllowedStuffsFor(thingDef))
    {
      if ((ext.allowedStuff == null || ext.allowedStuff.Contains(item)) && (godMode || GetAccessibleStuffCount(item, map) > 0))
      {
        var localStuffDef = item;
        string str = GenLabel.ThingLabel(thingDef, localStuffDef).CapitalizeFirst();
        var floatMenuOption = new FloatMenuOption(str, delegate
        {
          onSelected(localStuffDef);
        }, item);
        list.Add(floatMenuOption);
      }
    }

    if (list.Count == 0)
    {
      Messages.Message("NoStuffsToBuildWith".Translate(), MessageTypeDefOf.RejectInput, historical: false);
      return;
    }

    var floatMenu = new FloatMenu(list);
    Find.WindowStack.Add(floatMenu);
  }
}
