using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

using SolarWeb.Stratum.Things;

namespace SolarWeb.Stratum.AI.JobDrivers;

public class OperateRetractableRoofConsole : JobDriver
{
  private RetractableRoofConsole Console => (RetractableRoofConsole)job.targetA.Thing;

  public override bool TryMakePreToilReservations(bool errorOnFailed)
  {
    return pawn.Reserve(Console, job, 1, -1, null, errorOnFailed);
  }

  protected override IEnumerable<Toil> MakeNewToils()
  {
    this.FailOnDespawnedNullOrForbidden(TargetIndex.A);
    this.FailOn(() => Console.GetComp<CompPowerTrader>() != null && !Console.GetComp<CompPowerTrader>().PowerOn);
    this.FailOn(() => Console.isTransitioning);
    this.FailOn(() => !Console.jobPending);
    
    yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.InteractionCell);

    var operateToil = ToilMaker.MakeToil("OperateConsole");
    operateToil.initAction = () =>
    {
      pawn.pather.StopDead();
    };
    operateToil.tickAction = () =>
    {
      pawn.rotationTracker.FaceTarget(Console);
    };
    operateToil.defaultCompleteMode = ToilCompleteMode.Delay;
    operateToil.defaultDuration = 60;
    operateToil.WithProgressBarToilDelay(TargetIndex.A);
    operateToil.FailOnCannotTouch(TargetIndex.A, PathEndMode.InteractionCell);
    yield return operateToil;

    var finalizeToil = ToilMaker.MakeToil("InitiateTransition");
    finalizeToil.initAction = () =>
    {
      Console.InitiateTransition();
    };
    finalizeToil.defaultCompleteMode = ToilCompleteMode.Instant;
    yield return finalizeToil;
  }
}
