using RimWorld;
using Verse;
using Verse.AI;

namespace SolarWeb.Stratum.AI.WorkGivers;

public class FightRoofFires : WorkGiver_Scanner
{
  public override ThingRequest PotentialWorkThingRequest => ThingRequest.ForDef(DefOf.ThingDefOf.RoofFire);

  public override PathEndMode PathEndMode => PathEndMode.Touch;

  public override Danger MaxPathDanger(Pawn pawn)
  {
    return Danger.Deadly;
  }

  public override bool HasJobOnThing(Pawn pawn, Thing t, bool forced = false)
  {
    if (t is not Things.RoofFire fire)
    {
      return false;
    }

    if (pawn.WorkTagIsDisabled(WorkTags.Firefighting))
    {
      JobFailReason.Is("IncapableOfFirefighting".Translate());
      return false;
    }

    if (!forced && !pawn.Map.areaManager.Home[fire.Position])
    {
      JobFailReason.Is(WorkGiver_FixBrokenDownBuilding.NotInHomeAreaTrans);
      return false;
    }

    if ((pawn.Position - fire.Position).LengthHorizontalSquared > 225 && !pawn.CanReserve(fire, 1, -1, null, forced))
    {
      return false;
    }

    if (!forced && FireIsBeingHandled(fire, pawn))
    {
      return false;
    }

    return true;
  }

  public override Job JobOnThing(Pawn pawn, Thing t, bool forced = false)
  {
    return JobMaker.MakeJob(JobDefOf.BeatFire, t);
  }

  private static bool FireIsBeingHandled(Things.RoofFire f, Pawn potentialHandler)
  {
    if (!f.Spawned)
    {
      return false;
    }
    return f.Map.reservationManager.FirstRespectedReserver(f, potentialHandler)?.Position.InHorDistOf(f.Position, 5f) ?? false;
  }
}
