using System.Collections.Generic;
using HarmonyLib;
using Dubs_Skylight;
using Verse;
using RimWorld;

namespace SolarWeb.Stratum.DubsSkylights.Patches;

[HarmonyPatch(typeof(PlaceWorker_Skylight))]
public static class PlaceWorker_Skylight_Patch
{
  [HarmonyPatch(nameof(PlaceWorker_Skylight.AllowsPlacing))]
  [HarmonyPrefix]
  public static bool AllowsPlacing_Prefix(BuildableDef checkingDef, IntVec3 loc, Rot4 rot, Map map, Thing thingToIgnore, ref AcceptanceReport __result)
  {
    ThingDef? val = checkingDef as ThingDef;
    if (val != null && val.building.canPlaceOverWall)
    {
      __result = true;
      return false;
    }

    CellRect rect = GenAdj.OccupiedRect(loc, rot, checkingDef.Size);
    foreach (IntVec3 current in rect)
    {
      RoofDef roof = map.roofGrid.RoofAt(current);
      if (!IsRoofCompatibleWithSkylight(roof))
      {
        __result = new AcceptanceReport("SolarWeb_Stratum_DubsSkylights_IncompatibleRoof".Translate());
        return false;
      }

      if (current.Impassable(map))
      {
        __result = new AcceptanceReport("NotWithinRoomBounds".Translate());
        return false;
      }

      List<Thing> thingList = current.GetThingList(map);
      for (int i = 0; i < thingList.Count; i++)
      {
        Thing thing = thingList[i];
        if (thing == thingToIgnore) continue;

        if (thing is Building_skyLight)
        {
          __result = new AcceptanceReport("IdenticalThingExists".Translate());
          return false;
        }

        if (thing.def.entityDefToBuild == checkingDef)
        {
          if (thing is Blueprint)
          {
            __result = new AcceptanceReport("IdenticalBlueprintExists".Translate());
            return false;
          }
          __result = new AcceptanceReport("IdenticalThingExists".Translate());
          return false;
        }
      }
    }

    __result = true;
    return false;
  }

  private static bool IsRoofCompatibleWithSkylight(RoofDef roof)
  {
    if (roof == null) return false;
    return Stats.RoofStatCache.IsSkylight(roof);
  }
}
