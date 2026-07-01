using Verse;
using SolarWeb.Stratum.Stats;

namespace SolarWeb.Stratum.PlaceWorkers;

public class NotUnderRoofExceptSkylight : PlaceWorker
{
  public override AcceptanceReport AllowsPlacing(BuildableDef checkingDef, IntVec3 loc, Rot4 rot, Map map, Thing? thingToIgnore = null, Thing? thing = null)
  {
    if (checkingDef.Size.x == 1 && checkingDef.Size.z == 1)
    {
      var roof = map.roofGrid.RoofAt(loc);
      if (roof != null && !RoofStatCache.IsSkylight(roof))
      {
        return new AcceptanceReport("MustPlaceUnroofed".Translate());
      }
    }
    else
    {
      foreach (IntVec3 item in GenAdj.OccupiedRect(loc, rot, checkingDef.Size))
      {
        var roof = map.roofGrid.RoofAt(item);
        if (roof != null && !RoofStatCache.IsSkylight(roof))
        {
          return new AcceptanceReport("MustPlaceUnroofed".Translate());
        }
      }
    }
    return true;
  }
}
