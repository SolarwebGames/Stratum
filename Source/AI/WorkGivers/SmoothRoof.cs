using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace SolarWeb.Stratum.AI.WorkGivers;

public class SmoothRoof : WorkGiver_Scanner
{
  public override PathEndMode PathEndMode => PathEndMode.Touch;

  public override IEnumerable<IntVec3> PotentialWorkCellsGlobal(Pawn pawn)
  {
    foreach (var designator in pawn.Map.designationManager.SpawnedDesignationsOfDef(DefOf.DesignationDefOf.SmoothRoof))
    {
      yield return designator.target.Cell;
    }
  }

  public override bool ShouldSkip(Pawn pawn, bool forced = false)
  {
    return !pawn.Map.designationManager.AnySpawnedDesignationOfDef(DefOf.DesignationDefOf.SmoothRoof);
  }

  public override bool HasJobOnCell(Pawn pawn, IntVec3 c, bool forced = false)
  {
    if (pawn.Map.designationManager.DesignationAt(c, DefOf.DesignationDefOf.SmoothRoof) == null)
    {
      return false;
    }

    if (!pawn.CanReserve(c, 1, -1, ReservationLayerDefOf.Ceiling, forced))
    {
      return false;
    }

    return true;
  }

  public override Job? JobOnCell(Pawn pawn, IntVec3 c, bool forced = false)
  {
    return JobMaker.MakeJob(DefOf.JobDefOf.SmoothRoof, c);
  }
}
