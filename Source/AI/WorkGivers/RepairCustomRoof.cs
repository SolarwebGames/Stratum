using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

using SolarWeb.Stratum.MapComponents;

namespace SolarWeb.Stratum.AI.WorkGivers;

public class RepairCustomRoof : WorkGiver_Scanner
{
  public override PathEndMode PathEndMode => PathEndMode.Touch;

  public override Danger MaxPathDanger(Pawn pawn)
  {
    return Danger.Deadly;
  }

  public override IEnumerable<IntVec3> PotentialWorkCellsGlobal(Pawn pawn)
  {
    var map = pawn.Map;
    var grid = map.GetComponent<RoofIntegrityGrid>();
    if (grid != null)
    {
      var indices = map.cellIndices;
      foreach (var index in grid.RoofsNeedingRepair)
      {
        yield return indices.IndexToCell(index);
      }
    }
  }

  public override bool HasJobOnCell(Pawn pawn, IntVec3 cell, bool forced = false)
  {
    var map = pawn.Map;

    if (pawn.Faction == Faction.OfPlayer && !map.areaManager.Home[cell])
    {
      JobFailReason.Is("NotInHomeArea".Translate());
      return false;
    }

    var grid = map.GetComponent<RoofIntegrityGrid>();
    if (grid == null) return false;

    int index = map.cellIndices.CellToIndex(cell);
    if (!grid.RoofsNeedingRepair.Contains(index))
    {
      return false;
    }

    if (!pawn.CanReserve(cell, 1, -1, null, forced))
    {
      return false;
    }

    return true;
  }

  public override Job JobOnCell(Pawn pawn, IntVec3 cell, bool forced = false)
  {
    return JobMaker.MakeJob(DefOf.JobDefOf.RepairCustomRoof, cell);
  }
}
