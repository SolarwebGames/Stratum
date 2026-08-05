using Verse;

using SolarWeb.Stratum.DefModExtensions;
using SolarWeb.Stratum.Hooks;
using SolarWeb.Stratum.Utilities;

namespace SolarWeb.Stratum.AI.PlaceWorkers;

public class OnRoof : PlaceWorker
{
  public override AcceptanceReport AllowsPlacing(BuildableDef checkingDef, IntVec3 loc, Rot4 rot, Map map, Thing? thingToIgnore = null, Thing? thing = null)
  {
    if (checkingDef == null || map == null || map.roofGrid == null) return false;

    // Goes through the registry helper so global subscribers are consulted too. Walking only the
    // per-map handler list here meant compatibility modules -- which have no per-map hook point and
    // so always register globally -- were silently skipped.
    var hookResult = MapHookRegistry.CheckRoofBuildingPlacement(checkingDef, loc, rot, map);
    if (hookResult.HasValue) return hookResult.Value;

    var attachmentType = RoofBuildings.GetAttachmentType(checkingDef);
    var rect = GenAdj.OccupiedRect(loc, rot, checkingDef.Size);

    foreach (IntVec3 cell in rect)
    {
      if (!cell.InBounds(map)) return false;

      if (!map.roofGrid.Roofed(cell) || (map.areaManager?.NoRoof != null && map.areaManager.NoRoof[cell]))
      {
        return new AcceptanceReport("MustPlaceOnRoof".Translate());
      }

      var roof = map.roofGrid.RoofAt(cell);
      if (roof != null)
      {
        if (roof.isNatural && attachmentType == RoofAttachmentType.Rooftop)
        {
          return new AcceptanceReport("RoofAttachmentNotSupported".Translate());
        }

        var roofExt = roof.GetModExtension<BuildableRoofExtension>();
        if (roofExt != null)
        {
          if (attachmentType == RoofAttachmentType.Hanging && !roofExt.allowHangingAttachments)
          {
            return new AcceptanceReport("RoofAttachmentNotSupported".Translate());
          }
          if (attachmentType == RoofAttachmentType.Rooftop && !roofExt.allowRooftopAttachments)
          {
            return new AcceptanceReport("RoofAttachmentNotSupported".Translate());
          }
        }
      }

      var thingList = cell.GetThingList(map);
      for (int i = 0; i < thingList.Count; i++)
      {
        var t = thingList[i];
        if (t == thingToIgnore || t == thing) continue;

        if (RoofBuildings.IsRoofBuildingOrBlueprintOrFrame(t))
        {
          if (RoofBuildings.GetAttachmentType(t) == attachmentType)
          {
            return new AcceptanceReport("SpaceAlreadyOccupied".Translate());
          }
        }
      }
    }

    return true;
  }
}
