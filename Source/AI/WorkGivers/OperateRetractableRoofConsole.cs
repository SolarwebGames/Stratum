using RimWorld;
using Verse;
using Verse.AI;

using SolarWeb.Stratum.Things;

namespace SolarWeb.Stratum.AI.WorkGivers;

public class OperateRetractableRoofConsole : WorkGiver_Scanner
{
  public override ThingRequest PotentialWorkThingRequest => ThingRequest.ForDef(DefOf.ThingDefOf.RetractableRoofConsole);

  public override PathEndMode PathEndMode => PathEndMode.InteractionCell;

  public override bool HasJobOnThing(Pawn pawn, Thing t, bool forced = false)
  {
    if (t is not RetractableRoofConsole console) return false;
    
    if (!console.jobPending) return false;
    
    var power = console.GetComp<CompPowerTrader>();
    if (power != null && !power.PowerOn) return false;

    if (!pawn.CanReserve(t, 1, -1, null, forced)) return false;

    return true;
  }

  public override Job JobOnThing(Pawn pawn, Thing t, bool forced = false)
  {
    return JobMaker.MakeJob(DefOf.JobDefOf.OperateRetractableRoofConsole, t);
  }
}
