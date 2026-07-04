using Verse;
using RimWorld;
using SolarWeb.Stratum.Stats;

namespace SolarWeb.Stratum.JoyGivers;

public class UseTelescope : JoyGiver_InteractBuildingInteractionCell
{
  protected override bool CanInteractWith(Pawn pawn, Thing t, bool inBed)
  {
    if (def.unroofedOnly && t.Spawned)
    {
      var roof = t.Map.roofGrid.RoofAt(t.Position);
      if (roof != null && RoofStatCache.IsSkylight(roof) && RoofStatCache.GetTransparency(roof) > 0.5f)
      {
        bool originalUnroofedOnly = def.unroofedOnly;
        def.unroofedOnly = false;
        try
        {
          return base.CanInteractWith(pawn, t, inBed);
        }
        finally
        {
          def.unroofedOnly = originalUnroofedOnly;
        }
      }
    }

    return base.CanInteractWith(pawn, t, inBed);
  }
}
