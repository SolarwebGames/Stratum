using System.Collections.Generic;
using RimWorld;
using Verse;

using HediffDefOf = SolarWeb.Stratum.DefOf.HediffDefOf;

namespace SolarWeb.Stratum.Things;

public class BroadcastSpeaker : Building
{
  public const float MaxEffectRadius = 8.9f;
  private CompPowerTrader? powerComp;

  private static Dictionary<BroadcastProgram, HediffDef>? programHediffs;
  private static Dictionary<BroadcastProgram, HediffDef> ProgramHediffs => programHediffs ??= new()
  {
    { BroadcastProgram.Calming, HediffDefOf.PA_Calming_Hediff },
    { BroadcastProgram.Inspirational, HediffDefOf.PA_Inspirational_Hediff },
    { BroadcastProgram.Educational, HediffDefOf.PA_Educational_Hediff },
    { BroadcastProgram.Analytical, HediffDefOf.PA_Analytical_Hediff },
    { BroadcastProgram.SleepAid, HediffDefOf.PA_SleepAid_Hediff }
  };

  public override void SpawnSetup(Map map, bool respawningAfterLoad)
  {
    base.SpawnSetup(map, respawningAfterLoad);
    powerComp = GetComp<CompPowerTrader>();
  }

  public override void TickRare()
  {
    base.TickRare();
    if (powerComp == null || !powerComp.PowerOn) return;

    PowerNet net = powerComp.PowerNet;
    if (net == null) return;

    BroadcastTransmitter? transmitter = FindTransmitterOnNet(net);
    if (transmitter == null || transmitter.currentProgram == BroadcastProgram.None) return;

    Room room = Position.GetRoom(Map);
    if (room == null || room.TouchesMapEdge || room.UsesOutdoorTemperature) return;

    HediffDef? hediffDef = GetHediffDefForProgram(transmitter.currentProgram);
    if (hediffDef == null) return;

    var mapPawns = Map.mapPawns.AllPawnsSpawned;
    for (int i = 0; i < mapPawns.Count; i++)
    {
      Pawn pawn = mapPawns[i];
      if (pawn.GetRoom() == room && pawn.RaceProps.Humanlike && !pawn.Dead)
      {
        if (pawn.Position.DistanceTo(Position) <= MaxEffectRadius && GenSight.LineOfSight(Position, pawn.Position, Map))
        {
          ApplyProgramHediff(pawn, hediffDef, 350);
        }
      }
    }
  }

  public BroadcastTransmitter? connectedTransmitter;

  private BroadcastTransmitter? FindTransmitterOnNet(PowerNet net)
  {
    if (connectedTransmitter != null)
    {
      var transPower = connectedTransmitter.GetComp<CompPowerTrader>();
      if (transPower != null && transPower.PowerOn && transPower.PowerNet == net)
      {
        return connectedTransmitter;
      }
    }
    return null;
  }

  private HediffDef? GetHediffDefForProgram(BroadcastProgram program)
  {
    if (ProgramHediffs.TryGetValue(program, out var hediffDef))
    {
      return hediffDef;
    }
    return null;
  }

  private void ApplyProgramHediff(Pawn pawn, HediffDef hediffDef, int durationTicks)
  {
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

  public override string GetInspectString()
  {
    string baseText = base.GetInspectString();
    if (powerComp == null || !powerComp.PowerOn) return baseText;

    PowerNet net = powerComp.PowerNet;
    BroadcastTransmitter? transmitter = net != null ? FindTransmitterOnNet(net) : null;

    bool isConnected = transmitter != null;
    string connStatus = isConnected
      ? "SolarWeb_Stratum_SpeakerConnected".Translate()
      : "SolarWeb_Stratum_SpeakerDisconnected".Translate();

    string text = baseText + "\n" + connStatus;

    if (isConnected && transmitter!.currentProgram != BroadcastProgram.None)
    {
      string programName = transmitter.GetTranslatedProgramName(transmitter.currentProgram);
      string activeText = "SolarWeb_Stratum_ActiveProgram".Translate(programName);
      text += "\n" + activeText;

      if (!string.IsNullOrEmpty(transmitter.currentBroadcastDescription))
      {
        text += "\n" + transmitter.currentBroadcastDescription;
      }
    }

    return text;
  }

  public override void DrawExtraSelectionOverlays()
  {
    base.DrawExtraSelectionOverlays();
    GenDraw.DrawRadiusRing(Position, MaxEffectRadius);
  }
}
