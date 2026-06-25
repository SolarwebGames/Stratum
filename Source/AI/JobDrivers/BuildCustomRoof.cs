using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

using SolarWeb.Stratum.DefModExtensions;
using SolarWeb.Stratum.MapComponents;
using SolarWeb.Stratum.Things;

namespace SolarWeb.Stratum.AI.JobDrivers;

public class BuildCustomRoof : JobDriver
{
  protected IntVec3 Cell => TargetA.Cell;
  private RoofConstructionTracker? cachedTracker;

  public override bool TryMakePreToilReservations(bool errorOnFailed)
  {
    return pawn.Reserve(TargetA, job, 1, -1, null, errorOnFailed);
  }

  protected override IEnumerable<Toil> MakeNewToils()
  {
    this.FailOn(() => !pawn.CanReach(TargetA, PathEndMode.Touch, Danger.Deadly));
    this.FailOn(() =>
    {
      var frame = Cell.GetFirstThing<RoofFrame>(pawn.Map);
      return frame == null || frame.Faction != pawn.Faction || frame.IsForbidden(pawn);
    });
    this.FailOn(() => !RoofCollapseUtility.WithinRangeOfRoofHolder(Cell, pawn.Map));
    this.FailOn(() => !RoofCollapseUtility.ConnectedToRoofHolder(Cell, pawn.Map, assumeRoofAtRoot: true));

    yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.Touch);

    var build = ToilMaker.MakeToil("MakeNewToils");
    build.initAction = () =>
      {
        cachedTracker = pawn.Map.GetComponent<RoofConstructionTracker>();
        if (cachedTracker == null || !cachedTracker.TryGetRecord(Cell, out _))
        {
          EndJobWith(JobCondition.Incompletable);
        }
      };
    build.tickAction = () =>
        {
          if (cachedTracker != null && cachedTracker.TryGetRecord(Cell, out var rec))
          {
            pawn.rotationTracker.FaceCell(Cell);
            float work = pawn.GetStatValue(StatDefOf.ConstructionSpeed) * 1.7f;
            rec.workDone += work;
            pawn.skills?.Learn(SkillDefOf.Construction, 0.25f);

            if (rec.workDone >= rec.workTotal)
            {
              if (RoofUtility.FirstBlockingThing(Cell, pawn.Map) != null)
              {
                EndJobWith(JobCondition.Incompletable);
                return;
              }

              RoofDef roofDef = rec.roofDef;
              var ext = roofDef.GetModExtension<BuildableRoofExtension>();

              RoofMaterialUtils.ConsumeMaterialsAt(Cell, pawn.Map, ext);

              cachedTracker.CompleteConstruction(Cell);

              ReadyForNextToil();
            }
          }
          else
          {
            EndJobWith(JobCondition.Incompletable);
          }
        };
    build.defaultCompleteMode = ToilCompleteMode.Never;
    build.activeSkill = () => SkillDefOf.Construction;
    build.handlingFacing = true;

    build.WithProgressBar(TargetIndex.A, () =>
    {
      if (cachedTracker != null && cachedTracker.TryGetRecord(Cell, out var rec))
        return rec.workDone / rec.workTotal;
      return 0f;
    });
    yield return build;
  }
}
