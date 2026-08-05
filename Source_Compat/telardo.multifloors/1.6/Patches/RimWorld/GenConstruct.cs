using HarmonyLib;
using RimWorld;
using Verse;
using global::MultiFloors;

using SolarWeb.Stratum.DefModExtensions;
using SolarWeb.Stratum.Utilities;

namespace SolarWeb.Stratum.MultiFloors.Patches.RimWorld;

[HarmonyPatch(typeof(GenConstruct))]
public static class GenConstruct_Patch
{
  [HarmonyPatch(nameof(GenConstruct.CanPlaceBlueprintAt))]
  [HarmonyPrefix]
  public static bool CanPlaceBlueprintAt_Prefix(BuildableDef entDef, IntVec3 center, Rot4 rot, Map map, ref AcceptanceReport __result)
  {
    if (entDef != null && map != null && map.Level() > 0 && map.LowerMap() != null)
    {
      Map lowerMap = map.LowerMap();
      foreach (IntVec3 cell in GenAdj.OccupiedRect(center, rot, entDef.Size))
      {
        if (cell.InBounds(lowerMap))
        {
          var thingList = lowerMap.thingGrid.ThingsListAt(cell);
          for (int i = 0; i < thingList.Count; i++)
          {
            var t = thingList[i];
            if (RoofBuildings.IsRoofBuildingOrBlueprintOrFrame(t) &&
                RoofBuildings.GetAttachmentType(t) == RoofAttachmentType.Rooftop)
            {
              __result = new AcceptanceReport("Stratum_CannotBuildOverRooftopBuilding".Translate());
              return false;
            }
          }
        }
      }
    }
    return true;
  }
}
