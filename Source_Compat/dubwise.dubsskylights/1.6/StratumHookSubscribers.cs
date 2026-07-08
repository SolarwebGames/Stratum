using Verse;
using Dubs_Skylight;

namespace SolarWeb.Stratum.DubsSkylights;

internal static class StratumHookSubscribers
{
  public static void Register()
  {
    Utilities.StratumHooks.OnRoofChanged += (map, cell, oldRoof, newRoof) =>
    {
      if (map == null) return;
      bool oldIsSkylight = oldRoof != null && Stats.RoofStatCache.IsSkylight(oldRoof);
      bool newIsSkylight = newRoof != null && Stats.RoofStatCache.IsSkylight(newRoof);
      if (oldIsSkylight || newIsSkylight)
      {
        map.GetComponent<MapComp_Skylights>()?.RegenGrid();
      }
    };

    Utilities.StratumHooks.GetCellThermalConductivity += (map, cell, baseVal) =>
    {
      if (map == null || !cell.InBounds(map)) return baseVal;
      var roof = map.roofGrid.RoofAt(cell);
      if (roof == null || !Stats.RoofStatCache.IsSkylight(roof)) return baseVal;

      var thingList = map.thingGrid.ThingsListAt(cell);
      for (int i = 0; i < thingList.Count; i++)
      {
        var thing = thingList[i];
        if (thing is Dubs_Skylight.Building_skyLight)
        {
          return baseVal * 0.75f;
        }
      }
      return baseVal;
    };

    Utilities.StratumHooks.GetCellRoofMaxHitPoints += (map, cell, baseVal) =>
    {
      if (map == null || !cell.InBounds(map)) return baseVal;
      var roof = map.roofGrid.RoofAt(cell);
      if (roof == null || !Stats.RoofStatCache.IsSkylight(roof)) return baseVal;

      var thingList = map.thingGrid.ThingsListAt(cell);
      for (int i = 0; i < thingList.Count; i++)
      {
        var thing = thingList[i];
        if (thing is Dubs_Skylight.Building_skyLight)
        {
          return baseVal + 50;
        }
      }
      return baseVal;
    };

    Utilities.StratumHooks.GetCellRoofDamageThreshold += (map, cell, baseVal) =>
    {
      if (map == null || !cell.InBounds(map)) return baseVal;
      var roof = map.roofGrid.RoofAt(cell);
      if (roof == null || !Stats.RoofStatCache.IsSkylight(roof)) return baseVal;

      var thingList = map.thingGrid.ThingsListAt(cell);
      for (int i = 0; i < thingList.Count; i++)
      {
        var thing = thingList[i];
        if (thing is Dubs_Skylight.Building_skyLight)
        {
          return baseVal + 2.5f;
        }
      }
      return baseVal;
    };

    Utilities.StratumHooks.GetCellRoofArmorRating += (map, cell, baseVal) =>
    {
      if (map == null || !cell.InBounds(map)) return baseVal;
      var roof = map.roofGrid.RoofAt(cell);
      if (roof == null || !Stats.RoofStatCache.IsSkylight(roof)) return baseVal;

      var thingList = map.thingGrid.ThingsListAt(cell);
      for (int i = 0; i < thingList.Count; i++)
      {
        var thing = thingList[i];
        if (thing is Dubs_Skylight.Building_skyLight)
        {
          return baseVal + 0.1f;
        }
      }
      return baseVal;
    };
  }
}
