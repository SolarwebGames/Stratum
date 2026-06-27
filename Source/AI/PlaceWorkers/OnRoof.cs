using System;
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

    var registry = MapHookRegistry.Get(map);
    if (registry != null)
    {
      var handlers = registry.GetHandlers<MapHookRegistry.RoofBuildingPlacementCheckHandler>(MapHookRegistry.HookId.RoofBuildingPlacementCheck);
      if (handlers != null)
      {
        for (int i = 0; i < handlers.Count; i++)
        {
          try
          {
            var res = handlers[i](checkingDef, loc, map);
            if (res.HasValue) return res.Value;
          }
          catch (Exception ex)
          {
            StratumLog.Error($"Error in RoofBuildingPlacementCheck subscriber: {ex}");
          }
        }
      }
    }

    var attachmentType = RoofBuildings.GetAttachmentType(checkingDef);
    var rect = GenAdj.OccupiedRect(loc, rot, checkingDef.Size);

    foreach (IntVec3 cell in rect)
    {
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
    }

    return true;
  }
}
