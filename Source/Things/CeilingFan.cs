using RimWorld;
using Verse;
using HediffDefOf = SolarWeb.Stratum.DefOf.HediffDefOf;

namespace SolarWeb.Stratum.Things;

public class CeilingFan : Building
{
  public const float MaxEffectRadius = 4.9f;
  private CompPowerTrader? powerComp;

  public override void SpawnSetup(Map map, bool respawningAfterLoad)
  {
    base.SpawnSetup(map, respawningAfterLoad);
    powerComp = GetComp<CompPowerTrader>();
  }

  public override void TickRare()
  {
    base.TickRare();
    if (powerComp != null && !powerComp.PowerOn) return;

    Room room = Position.GetRoom(Map);
    if (room == null) return;

    var roomPawns = room.ContainedThings<Pawn>();
    foreach (var pawn in roomPawns)
    {
      if (pawn.GetRoom() == room && pawn.RaceProps.Humanlike && !pawn.Dead)
      {
        if (pawn.Position.DistanceTo(Position) <= MaxEffectRadius && GenSight.LineOfSight(Position, pawn.Position, Map))
        {
          ApplyBreezeHediff(pawn, HediffDefOf.CeilingFanBreeze, 350);
        }
      }
    }
  }

  private void ApplyBreezeHediff(Pawn pawn, HediffDef hediffDef, int durationTicks)
  {
    if (pawn.health == null || pawn.health.hediffSet == null) return;

    Hediff existing = pawn.health.hediffSet.GetFirstHediffOfDef(hediffDef);
    if (existing == null)
    {
      Hediff hediff = HediffMaker.MakeHediff(hediffDef, pawn);
      var disappearsComp = hediff.TryGetComp<HediffComp_Disappears>();
      if (disappearsComp != null)
      {
        disappearsComp.ticksToDisappear = durationTicks;
      }
      pawn.health.AddHediff(hediff);
    }
    else
    {
      var disappearsComp = existing.TryGetComp<HediffComp_Disappears>();
      if (disappearsComp != null)
      {
        disappearsComp.ticksToDisappear = durationTicks;
      }
    }
  }

  public override void DrawExtraSelectionOverlays()
  {
    base.DrawExtraSelectionOverlays();
    GenDraw.DrawRadiusRing(Position, MaxEffectRadius);
  }
}
