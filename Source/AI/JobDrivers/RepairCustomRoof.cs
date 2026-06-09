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
    this.FailOn(() => !pawn.CanReach(Cell, PathEndMode.ClosestTouch, Danger.Deadly));
    this.FailOn(() => pawn.Faction == Faction.OfPlayer && !pawn.Map.areaManager.Home[Cell]);

    yield return Toils_Goto.GotoCell(TargetIndex.A, PathEndMode.ClosestTouch);

    Toil repair = new Toil();
    repair.initAction = delegate
    {
      ticksToNextRepair = 80f;
      cachedGrid = pawn.Map.GetComponent<RoofIntegrityGrid>();
    };
    repair.tickAction = delegate
    {
      Pawn actor = repair.actor;
      actor.skills?.Learn(SkillDefOf.Construction, 0.05f);

      float num = actor.GetStatValue(StatDefOf.ConstructionSpeed) * 1.7f;
      ticksToNextRepair -= num;

      if (ticksToNextRepair <= 0f)
      {
        ticksToNextRepair += TicksBetweenRepairs;

        if (cachedGrid != null)
        {
          cachedGrid.Repair(Cell, 1);

          if (cachedGrid.GetHitPoints(Cell) >= cachedGrid.GetMaxHitPoints(Cell))
          {
            actor.records.Increment(RecordDefOf.ThingsRepaired);
            actor.jobs.EndCurrentJob(JobCondition.Succeeded);
          }
        }
      }
    };

    repair.FailOnCannotTouch(TargetIndex.A, PathEndMode.ClosestTouch);
    repair.WithEffect(EffecterDefOf.ConstructMetal, TargetIndex.A);
    repair.defaultCompleteMode = ToilCompleteMode.Never;
    repair.activeSkill = () => SkillDefOf.Construction;

    yield return repair;
  }
}
