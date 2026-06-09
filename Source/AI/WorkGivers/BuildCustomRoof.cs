using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

using SolarWeb.Stratum.Things;

namespace SolarWeb.Stratum.AI.WorkGivers;

public class BuildCustomRoof : WorkGiver_Scanner
{
  public override PathEndMode PathEndMode => PathEndMode.Touch;

  public override IEnumerable<Thing> PotentialWorkThingsGlobal(Pawn pawn)
  {
    foreach (var t in pawn.Map.listerThings.ThingsOfDef(DefOf.ThingDefOf.RoofFrame))
      yield return t;
  }

  public override Job? JobOnCell(Pawn pawn, IntVec3 cell, bool forced = false)
  {
    var frame = cell.GetFirstThing<RoofFrame>(pawn.Map);
    if (frame == null) return null;
    return JobOnThing(pawn, frame, forced);
  }

  public override Job? JobOnThing(Pawn pawn, Thing t, bool forced = false)
  {
    if (t is not RoofFrame frame) return null;
    if (frame.Faction != pawn.Faction) return null!;
    if (!pawn.CanReserve(frame, 1, -1, null, forced)) return null!;

    if (!RoofCollapseUtility.WithinRangeOfRoofHolder(frame.Position, pawn.Map) ||
        !RoofCollapseUtility.ConnectedToRoofHolder(frame.Position, pawn.Map, assumeRoofAtRoot: true))
    {
      return null!;
    }

    if (!frame.IsCompleted()) return null;

    Thing blocker = RoofUtility.FirstBlockingThing(frame.Position, pawn.Map);
    if (blocker != null)
    {
      return RoofUtility.HandleBlockingThingJob(blocker, pawn, forced);
    }

    return JobMaker.MakeJob(DefOf.JobDefOf.BuildCustomRoof, t);
  }
}
