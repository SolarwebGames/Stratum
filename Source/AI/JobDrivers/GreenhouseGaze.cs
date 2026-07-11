using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

using SolarWeb.Stratum.Stats;

namespace SolarWeb.Stratum.AI.JobDrivers;

public class GreenhouseGaze : JobDriver
{
  protected IntVec3 TargetCell => TargetA.Cell;

  public override bool TryMakePreToilReservations(bool errorOnFailed)
  {
    return true;
  }

  private List<Pawn> GetAdjacentCompanions()
  {
    List<Pawn> list = [];
    var map = pawn.Map;
    if (map == null) return list;

    var pos = pawn.Position;
    var room = pawn.GetRoom();
    if (room == null) return list;

    for (int i = 0; i < 4; i++)
    {
      var adjCell = pos + GenAdj.CardinalDirections[i];
      if (adjCell.InBounds(map) && adjCell.GetRoom(map) == room)
      {
        var things = adjCell.GetThingList(map);
        for (int j = 0; j < things.Count; j++)
        {
          if (things[j] is Pawn otherPawn && otherPawn != pawn && otherPawn.CurJob?.def == job.def)
          {
            list.Add(otherPawn);
            break;
          }
        }
      }
    }
    return list;
  }

  protected override IEnumerable<Toil> MakeNewToils()
  {
    yield return Toils_Goto.GotoCell(TargetIndex.A, PathEndMode.OnCell);

    Toil gazeToil = ToilMaker.MakeToil("MakeNewToils");
    gazeToil.initAction = delegate
    {
      pawn.jobs.posture = PawnPosture.LayingOnGroundFaceUp;
    };
    gazeToil.tickIntervalAction = delegate (int delta)
    {
      var map = pawn.Map;
      if (map == null) return;

      var companions = GetAdjacentCompanions();
      if (companions.Count > 0)
      {
        pawn.skills?.Learn(SkillDefOf.Social, 0.05f * delta);
      }

      float transparency = 1f;
      var roof = pawn.Position.GetRoof(map);
      if (roof != null)
      {
        if (RoofStatCache.IsSkylight(roof))
        {
          transparency = RoofStatCache.GetTransparency(roof);
        }
        else
        {
          transparency = 0f;
        }
      }

      float weatherFactor = 1.0f;
      float finalFactor = transparency * weatherFactor;

      JoyUtility.JoyTickCheckEnd(pawn, delta, JoyTickFullJoyAction.EndJob, finalFactor);
    };

    gazeToil.defaultCompleteMode = ToilCompleteMode.Delay;
    gazeToil.defaultDuration = job.def.joyDuration;

    gazeToil.AddFinishAction(delegate
    {
      var companions = GetAdjacentCompanions();
      if (companions.Count >= 3)
      {
        TaleRecorder.RecordTale(DefOf.TaleDefOf.GreenhouseGazedTogether, pawn, companions[0], companions[1], companions[2]);
      }
    });

    gazeToil.FailOn(() => pawn.story?.traits?.HasTrait(TraitDefOf.Undergrounder) == true);

    gazeToil.FailOn(() =>
    {
      var room = pawn.GetRoom();
      return room == null || room.Role != DefOf.RoomRoleDefOf.Greenhouse;
    });

    gazeToil.FailOn(() =>
    {
      var room = pawn.GetRoom();
      return room == null || !HasPlants(room);
    });

    gazeToil.FailOn(() =>
    {
      var roof = pawn.Position.GetRoof(pawn.Map);
      return roof != null && !RoofStatCache.IsSkylight(roof);
    });

    yield return gazeToil;
  }

  private bool HasPlants(Room room)
  {
    var map = room.Map;
    if (map == null) return false;

    foreach (var cell in room.Cells)
    {
      List<Thing> things = cell.GetThingList(map);
      for (int i = 0; i < things.Count; i++)
      {
        if (things[i] is Plant)
        {
          return true;
        }
      }
    }

    return false;
  }

  public override string GetReport()
  {
    var companions = GetAdjacentCompanions();

    if (companions.Count == 1)
    {
      return "Stratum_GreenhouseGazingReportTogether".Translate(companions[0].LabelShort);
    }
    if (companions.Count == 2)
    {
      return "Stratum_GreenhouseGazingReportTogether2".Translate(companions[0].LabelShort, companions[1].LabelShort);
    }
    if (companions.Count >= 3)
    {
      return "Stratum_GreenhouseGazingReportTogether3".Translate(companions[0].LabelShort, companions[1].LabelShort, companions[2].LabelShort);
    }

    return "Stratum_GreenhouseGazingReport".Translate();
  }
}
