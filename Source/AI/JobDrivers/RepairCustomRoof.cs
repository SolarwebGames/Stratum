using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

using SolarWeb.Stratum.MapComponents;

namespace SolarWeb.Stratum.AI.JobDrivers;

public class RepairCustomRoof : JobDriver
{
  protected float ticksToNextRepair;
  private const float TicksBetweenRepairs = 20f;
  protected IntVec3 Cell => TargetA.Cell;
  private RoofIntegrityGrid? cachedGrid;

  public override bool TryMakePreToilReservations(bool errorOnFailed)
  {
    return pawn.Reserve(Cell, job, 1, -1, null, errorOnFailed);
  }

  protected override IEnumerable<Toil> MakeNewToils()
  {
    this.FailOn(() => !pawn.CanReach(Cell, PathEndMode.Touch, Danger.Deadly));
    this.FailOn(() => pawn.Faction == Faction.OfPlayer && !pawn.Map.areaManager.Home[Cell] && !job.playerForced);

    yield return Toils_Goto.GotoCell(TargetIndex.A, PathEndMode.Touch);

    Toil repair = ToilMaker.MakeToil("MakeNewToils");
    repair.defaultCompleteMode = ToilCompleteMode.Never;
    repair.activeSkill = () => SkillDefOf.Construction;
    repair.handlingFacing = true;
    repair.initAction = delegate
        {
          ticksToNextRepair = 80f;
          cachedGrid = pawn.Map.GetComponent<RoofIntegrityGrid>();
        };
    repair.tickAction = delegate
        {
          pawn.skills?.Learn(SkillDefOf.Construction, 0.05f);
          pawn.rotationTracker.FaceCell(Cell);

          float num = pawn.GetStatValue(StatDefOf.ConstructionSpeed) * 1.7f;
          ticksToNextRepair -= num;

          if (ticksToNextRepair <= 0f)
          {
            ticksToNextRepair += TicksBetweenRepairs;

            if (cachedGrid != null)
            {
              cachedGrid.Repair(Cell, 1);

              if (cachedGrid.GetHitPoints(Cell) >= cachedGrid.GetMaxHitPoints(Cell))
              {
                pawn.records.Increment(RecordDefOf.ThingsRepaired);
                pawn.jobs.EndCurrentJob(JobCondition.Succeeded);
              }
            }
          }
        };
    repair.FailOnCannotTouch(TargetIndex.A, PathEndMode.Touch);
    repair.WithEffect(EffecterDefOf.ConstructMetal, TargetIndex.A);

    yield return repair;
  }
}
