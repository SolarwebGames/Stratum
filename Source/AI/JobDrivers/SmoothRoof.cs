using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace SolarWeb.Stratum.AI.JobDrivers;

public class SmoothRoof : JobDriver
{
  private float workLeft = -1000f;
  protected int BaseWorkAmount => 2800;
  protected DesignationDef DesDef => DefOf.DesignationDefOf.SmoothRoof;
  protected StatDef SpeedStat => StatDefOf.SmoothingSpeed;

  public override bool TryMakePreToilReservations(bool errorOnFailed)
  {
    return pawn.Reserve(job.targetA, job, 1, -1, ReservationLayerDefOf.Ceiling, errorOnFailed);
  }

  protected override IEnumerable<Toil> MakeNewToils()
  {
    this.FailOn(() => (!job.ignoreDesignations && Map.designationManager.DesignationAt(TargetLocA, DesDef) == null));
    yield return Toils_Goto.GotoCell(TargetIndex.A, PathEndMode.Touch);

    Toil doWork = ToilMaker.MakeToil("MakeNewToils");
    doWork.initAction = delegate
    {
      workLeft = BaseWorkAmount;
    };
    doWork.tickIntervalAction = delegate (int delta)
    {
      float num = ((SpeedStat != null && !SpeedStat.Worker.IsDisabledFor(doWork.actor)) ? doWork.actor.GetStatValue(SpeedStat) : 1f);
      num *= 1.7f * delta;
      workLeft -= num;
      if (doWork.actor.skills != null)
      {
        doWork.actor.skills.Learn(SkillDefOf.Construction, 0.1f * delta);
      }
      if (workLeft <= 0f)
      {
        DoEffect(TargetLocA);
        Map.designationManager.DesignationAt(TargetLocA, DesDef)?.Delete();
        ReadyForNextToil();
      }
    };
    doWork.FailOnCannotTouch(TargetIndex.A, PathEndMode.Touch);
    doWork.WithProgressBar(TargetIndex.A, () => 1f - workLeft / (float)BaseWorkAmount);
    doWork.defaultCompleteMode = ToilCompleteMode.Never;
    doWork.activeSkill = () => SkillDefOf.Construction;
    yield return doWork;
  }

  private void DoEffect(IntVec3 c)
  {
    var roof = Map.roofGrid.RoofAt(c);
    var smoothedRoof = GetSmoothedVersion(roof);
    if (smoothedRoof != roof)
    {
      Map.roofGrid.SetRoof(c, smoothedRoof);
      FleckMaker.ThrowMetaPuffs(new TargetInfo(c, Map));
    }
  }

  private static RoofDef GetSmoothedVersion(RoofDef original)
  {
    if (original == RoofDefOf.RoofRockThin)
      return DefOf.RoofDefOf.RoofThinRockSmoothed;
    if (original == RoofDefOf.RoofRockThick)
      return DefOf.RoofDefOf.RoofOverheadMountainSmoothed;
    return original;
  }

  public override void ExposeData()
  {
    base.ExposeData();
    Scribe_Values.Look(ref workLeft, "workLeft", 0f);
  }
}
