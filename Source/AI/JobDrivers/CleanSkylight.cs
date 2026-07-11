using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

using SolarWeb.Stratum.MapComponents;

namespace SolarWeb.Stratum.AI.JobDrivers;

public class CleanSkylight : JobDriver
{
  protected IntVec3 Cell => TargetA.Cell;

  public override bool TryMakePreToilReservations(bool errorOnFailed)
  {
    return pawn.Reserve(Cell, job, 1, -1, null, errorOnFailed);
  }

  protected override IEnumerable<Toil> MakeNewToils()
  {
    this.FailOn(() => !pawn.CanReach(Cell, PathEndMode.OnCell, Danger.Deadly));
    this.FailOn(() => pawn.Faction == Faction.OfPlayer && !pawn.Map.areaManager.Home[Cell] && !job.playerForced);

    yield return Toils_Goto.GotoCell(TargetIndex.A, PathEndMode.OnCell);

    Toil clean = ToilMaker.MakeToil("MakeNewToils");
    clean.defaultCompleteMode = ToilCompleteMode.Delay;
    clean.defaultDuration = 120;
    clean.handlingFacing = true;
    clean.initAction = delegate
    {
      pawn.rotationTracker.FaceCell(Cell);
    };
    clean.tickAction = delegate
    {
      pawn.rotationTracker.FaceCell(Cell);
      if (pawn.IsHashIntervalTick(15))
      {
        FleckMaker.ThrowSmoke(Cell.ToVector3Shifted(), pawn.Map, 0.4f);
      }
    };
    clean.AddFinishAction(delegate
    {
      var dirt = pawn.Map?.GetComponent<SkylightCoating>();
      if (dirt != null)
      {
        dirt.SetDirtLevel(Cell, 0f);
        dirt.SetPollenLevel(Cell, 0f);
        dirt.SetSnowLevel(Cell, 0f);
      }
    });
    clean.FailOnCannotTouch(TargetIndex.A, PathEndMode.OnCell);
    clean.WithEffect(EffecterDefOf.Clean, TargetIndex.A);

    yield return clean;
  }
}
