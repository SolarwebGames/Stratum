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
          cachedGrid = pawn.Map?.GetComponent<RoofIntegrityGrid>();
        };
    repair.tickAction = delegate
        {
          Pawn actor = repair.actor;
          actor.skills?.Learn(SkillDefOf.Construction, 0.05f);
          actor.rotationTracker.FaceCell(Cell);

          float num = (StatDefOf.ConstructionSpeed.Worker.IsDisabledFor(actor) ? 0.1f : actor.GetStatValue(StatDefOf.ConstructionSpeed)) * 1.7f;
          ticksToNextRepair -= num;

          if (ticksToNextRepair <= 0f)
          {
            ticksToNextRepair += TicksBetweenRepairs;

            var grid = cachedGrid ?? actor.Map?.GetComponent<RoofIntegrityGrid>();
            if (grid != null)
            {
              grid.Repair(Cell, 1);

              if (grid.GetHitPoints(Cell) >= grid.GetMaxHitPoints(Cell))
              {
                actor.records.Increment(RecordDefOf.ThingsRepaired);
                actor.jobs.EndCurrentJob(JobCondition.Succeeded);
              }
            }
          }
        };
    repair.FailOnCannotTouch(TargetIndex.A, PathEndMode.Touch);
    repair.WithEffect(EffecterDefOf.ConstructMetal, TargetIndex.A);

    yield return repair;
  }
}
