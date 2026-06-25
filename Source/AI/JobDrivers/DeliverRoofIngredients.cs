using System.Collections.Generic;
using UnityEngine;
using Verse;
using Verse.AI;

using SolarWeb.Stratum.Things;
using RimWorld;

namespace SolarWeb.Stratum.AI.JobDrivers;

public class DeliverRoofIngredients : JobDriver
{
  private const TargetIndex StackInd = TargetIndex.A;
  private const TargetIndex FrameInd = TargetIndex.B;

  protected RoofFrame CurrentFrame => (RoofFrame)job.GetTarget(FrameInd).Thing;

  public override bool TryMakePreToilReservations(bool errorOnFailed)
  {
    if (!pawn.Reserve(job.GetTarget(StackInd), job, 1, -1, null, errorOnFailed))
      return false;

    if (!pawn.Reserve(job.GetTarget(FrameInd), job, 1, -1, null, errorOnFailed))
      return false;

    pawn.ReserveAsManyAsPossible(job.GetTargetQueue(StackInd), job, 1, -1, null);
    pawn.ReserveAsManyAsPossible(job.GetTargetQueue(FrameInd), job, 1, -1, null);

    return true;
  }

  protected override IEnumerable<Toil> MakeNewToils()
  {
    this.FailOn(() => CurrentFrame == null || CurrentFrame.Faction != pawn.Faction || CurrentFrame.IsForbidden(pawn));

    Toil gotoStack = Toils_Goto.GotoThing(StackInd, PathEndMode.ClosestTouch)
      .FailOnDespawnedNullOrForbidden(StackInd)
      .FailOnSomeonePhysicallyInteracting(StackInd);
    yield return gotoStack;

    yield return Toils_Haul.StartCarryThing(StackInd,
      putRemainderInQueue: false,
      subtractNumTakenFromJobCount: true,
      failIfStackCountLessThanJobCount: false);

    yield return Toils_Haul.JumpIfAlsoCollectingNextTargetInQueue(gotoStack, StackInd);

    Toil gotoFrame = Toils_Goto.GotoThing(FrameInd, PathEndMode.ClosestTouch);
    yield return gotoFrame;

    Toil deposit = ToilMaker.MakeToil("DepositRoofMaterials");
    deposit.initAction = delegate
    {
      var carried = pawn.carryTracker.CarriedThing;
      if (carried == null) { EndJobWith(JobCondition.Succeeded); return; }

      int needed = CurrentFrame.ThingCountNeeded(carried.def);
      int transferCount = Mathf.Min(carried.stackCount, needed);

      if (transferCount > 0)
      {
        CurrentFrame.resourceContainer.TryAddOrTransfer(carried, transferCount);
      }

      if (pawn.carryTracker.CarriedThing != null && !job.GetTargetQueue(FrameInd).NullOrEmpty())
      {
        var queue = job.GetTargetQueue(FrameInd);
        job.SetTarget(FrameInd, queue[0].Thing);
        queue.RemoveAt(0);
        pawn.jobs.curDriver.JumpToToil(gotoFrame);
      }
      else
      {
        EndJobWith(JobCondition.Succeeded);
      }
    };
    yield return deposit;
  }
}
