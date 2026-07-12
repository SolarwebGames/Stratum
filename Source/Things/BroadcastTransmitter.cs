using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;
using Verse.Grammar;

namespace SolarWeb.Stratum.Things;

public enum BroadcastProgram
{
  None,
  Calming,
  Inspirational,
  Educational,
  Analytical,
  SleepAid
}

public class BroadcastTransmitter : Building
{
  public BroadcastProgram currentProgram = BroadcastProgram.None;
  public string currentBroadcastDescription = "";
  private int ticksUntilNextDescription = 0;
  private CompPowerTrader? powerComp;

  private Texture2D? icon;

  private static readonly Dictionary<BroadcastProgram, string> programTranslationKeys = new()
  {
    { BroadcastProgram.None, "SolarWeb_Stratum_Program_None" },
    { BroadcastProgram.Calming, "SolarWeb_Stratum_Program_Calming" },
    { BroadcastProgram.Inspirational, "SolarWeb_Stratum_Program_Inspirational" },
    { BroadcastProgram.Educational, "SolarWeb_Stratum_Program_Educational" },
    { BroadcastProgram.Analytical, "SolarWeb_Stratum_Program_Analytical" },
    { BroadcastProgram.SleepAid, "SolarWeb_Stratum_Program_SleepAid" }
  };

  private static readonly Dictionary<BroadcastProgram, RulePackDef> rulePackMaps = new()
  {
    { BroadcastProgram.Calming, DefOf.RulePackDefOf.PA_Calming_RulePack },
    { BroadcastProgram.Inspirational, DefOf.RulePackDefOf.PA_Inspirational_RulePack },
    { BroadcastProgram.Educational, DefOf.RulePackDefOf.PA_Educational_RulePack },
    { BroadcastProgram.Analytical, DefOf.RulePackDefOf.PA_Analytical_RulePack },
    { BroadcastProgram.SleepAid, DefOf.RulePackDefOf.PA_SleepAid_RulePack }
  };

  public override void SpawnSetup(Map map, bool respawningAfterLoad)
  {
    base.SpawnSetup(map, respawningAfterLoad);
    powerComp = GetComp<CompPowerTrader>();
    if (string.IsNullOrEmpty(currentBroadcastDescription) && currentProgram != BroadcastProgram.None)
    {
      GenerateBroadcastDescription();
    }
  }

  public override void ExposeData()
  {
    base.ExposeData();
    Scribe_Values.Look(ref currentProgram, "currentProgram", BroadcastProgram.None);
    Scribe_Values.Look(ref currentBroadcastDescription, "currentBroadcastDescription", "");
    Scribe_Values.Look(ref ticksUntilNextDescription, "ticksUntilNextDescription", 0);
  }

  public override void TickRare()
  {
    base.TickRare();
    if (powerComp == null || !powerComp.PowerOn) return;

    if (powerComp.PowerNet != null)
    {
      for (int i = 0; i < powerComp.PowerNet.connectors.Count; i++)
      {
        CompPower connector = powerComp.PowerNet.connectors[i];
        if (connector != null && connector.parent is BroadcastSpeaker speaker)
        {
          speaker.connectedTransmitter = this;
        }
      }
    }

    if (currentProgram != BroadcastProgram.None)
    {
      ticksUntilNextDescription -= 250;
      if (ticksUntilNextDescription <= 0 || string.IsNullOrEmpty(currentBroadcastDescription))
      {
        GenerateBroadcastDescription();
        ticksUntilNextDescription = Rand.Range(2500, 5000);
      }
    }
  }

  public override IEnumerable<Gizmo> GetGizmos()
  {
    foreach (var gizmo in base.GetGizmos())
    {
      yield return gizmo;
    }

    if (icon == null)
    {
      icon = ContentFinder<Texture2D>.Get("UI/Icons/Options/OptionsAudio", false);
    }

    if (powerComp != null && powerComp.PowerOn)
    {
      yield return new Command_Action
      {
        defaultLabel = "SolarWeb_Stratum_BroadcastProgramLabel".Translate(GetTranslatedProgramName(currentProgram)),
        defaultDesc = "SolarWeb_Stratum_BroadcastTransmitterDesc".Translate(),
        icon = icon ?? BaseContent.BadTex,
        action = OpenProgramSelectionMenu
      };
    }
  }

  private void OpenProgramSelectionMenu()
  {
    List<FloatMenuOption> options = new List<FloatMenuOption>();
    foreach (BroadcastProgram program in System.Enum.GetValues(typeof(BroadcastProgram)))
    {
      BroadcastProgram localProgram = program;
      options.Add(new FloatMenuOption(GetTranslatedProgramName(localProgram), () =>
      {
        SetProgram(localProgram);
      }));
    }
    Find.WindowStack.Add(new FloatMenu(options));
  }

  private void SetProgram(BroadcastProgram newProgram)
  {
    if (currentProgram == newProgram) return;

    currentProgram = newProgram;
    GenerateBroadcastDescription();
    ticksUntilNextDescription = Rand.Range(2500, 5000);

    if (SoundDefOf.Click != null)
    {
      SoundDefOf.Click.PlayOneShotOnCamera();
    }
  }

  public string GetTranslatedProgramName(BroadcastProgram program)
  {
    if (programTranslationKeys.TryGetValue(program, out var key))
    {
      return key.Translate();
    }
    return "SolarWeb_Stratum_Program_None".Translate();
  }

  private void GenerateBroadcastDescription()
  {
    if (currentProgram == BroadcastProgram.None)
    {
      currentBroadcastDescription = "";
      return;
    }

    var rulePackFound = rulePackMaps.TryGetValue(currentProgram, out var pack);
    if (rulePackFound)
    {
      var request = new GrammarRequest();
      request.Includes.Add(pack);
      currentBroadcastDescription = GrammarResolver.Resolve("root", request);
    }
    else
    {
      currentBroadcastDescription = "";
    }
  }

  public override string GetInspectString()
  {
    string baseText = base.GetInspectString();
    if (powerComp != null && !powerComp.PowerOn) return baseText;

    string programName = GetTranslatedProgramName(currentProgram);
    string activeText = "SolarWeb_Stratum_ActiveProgram".Translate(programName);
    string connectionText = "SolarWeb_Stratum_ConnectedSpeakersCount".Translate(GetConnectedSpeakersCount());

    string text = baseText + "\n" + activeText + "\n" + connectionText;

    if (currentProgram != BroadcastProgram.None && !string.IsNullOrEmpty(currentBroadcastDescription))
    {
      text += "\n" + currentBroadcastDescription;
    }
    return text;
  }

  private int GetConnectedSpeakersCount()
  {
    if (powerComp == null || powerComp.PowerNet == null) return 0;
    int count = 0;
    PowerNet net = powerComp.PowerNet;
    for (int i = 0; i < net.connectors.Count; i++)
    {
      CompPower connector = net.connectors[i];
      if (connector != null && connector.parent is BroadcastSpeaker speaker)
      {
        var speakerPower = speaker.GetComp<CompPowerTrader>();
        if (speakerPower != null && speakerPower.PowerOn)
        {
          count++;
        }
      }
    }
    return count;
  }

  public override void DrawExtraSelectionOverlays()
  {
    base.DrawExtraSelectionOverlays();
    if (powerComp == null || powerComp.PowerNet == null) return;

    PowerNet net = powerComp.PowerNet;
    for (int i = 0; i < net.connectors.Count; i++)
    {
      CompPower connector = net.connectors[i];
      if (connector != null && connector.parent is BroadcastSpeaker speaker)
      {
        GenDraw.DrawRadiusRing(speaker.Position, BroadcastSpeaker.MaxEffectRadius);
      }
    }
  }
}
