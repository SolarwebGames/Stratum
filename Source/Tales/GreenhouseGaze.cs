using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using Verse.Grammar;

namespace SolarWeb.Stratum.Tales;

public class GreenhouseGaze : Tale
{
  public List<TaleData_Pawn> participantsData = [];
  public WeatherDef? weather;

  public override Pawn DominantPawn => participantsData.Count > 0 ? participantsData[0].pawn : null!;

  public override string ShortSummary
  {
    get
    {
      if (!customLabel.NullOrEmpty())
      {
        return customLabel.CapitalizeFirst();
      }
      string summary = def.LabelCap;
      if (participantsData.Count > 0)
      {
        summary += ": " + string.Join(", ", participantsData.Select(p => p.name?.ToString() ?? ""));
      }
      return summary;
    }
  }

  public GreenhouseGaze() : base() { }

  public GreenhouseGaze(Pawn p1, Pawn p2) : this([p1, p2]) { }
  public GreenhouseGaze(Pawn p1, Pawn p2, Pawn p3) : this([p1, p2, p3]) { }
  public GreenhouseGaze(Pawn p1, Pawn p2, Pawn p3, Pawn p4) : this([p1, p2, p3, p4]) { }

  public GreenhouseGaze(List<Pawn> pawns) : base()
  {
    foreach (var pawn in pawns)
    {
      if (pawn != null)
      {
        participantsData.Add(TaleData_Pawn.GenerateFrom(pawn));
      }
    }

    if (pawns.Count > 0 && pawns[0] != null)
    {
      var map = pawns[0].MapHeld;
      if (pawns[0].SpawnedOrAnyParentSpawned)
      {
        surroundings = TaleData_Surroundings.GenerateFrom(pawns[0].PositionHeld, map);
      }
      if (map != null)
      {
        weather = map.weatherManager.CurWeatherPerceived;
      }
    }
  }

  public override bool Concerns(Thing th)
  {
    if (base.Concerns(th)) return true;
    foreach (var data in participantsData)
    {
      if (data.pawn == th) return true;
    }
    return false;
  }

  public override void Notify_FactionRemoved(Faction faction)
  {
    base.Notify_FactionRemoved(faction);
    foreach (var data in participantsData)
    {
      if (data.faction == faction)
      {
        data.faction = null;
      }
    }
  }

  public override void ExposeData()
  {
    base.ExposeData();
    Scribe_Collections.Look(ref participantsData, "participantsData", LookMode.Deep);
    Scribe_Defs.Look(ref weather, "weather");
  }

  protected override IEnumerable<Rule> SpecialTextGenerationRules(Dictionary<string, string>? outConstants = null)
  {
    foreach (var rule in base.SpecialTextGenerationRules(outConstants))
    {
      yield return rule;
    }

    if (weather != null)
    {
      yield return new Rule_String("WEATHER_label", weather.label);

      string desc = "a changing sky";
      if (weather.rainRate > 0.1f)
      {
        desc = "rain pattering against the glass panes";
        if (weather.defName != null && (weather.defName.Contains("Thunderstorm") || weather.defName.Contains("Storm")))
        {
          desc = "lightning flashes illuminating the glass roof during a storm";
        }
      }
      else if (weather.snowRate > 0.1f)
      {
        desc = "snow drifting onto the glass panels";
      }
      else if (weather.defName != null)
      {
        if (weather.defName.Contains("Clear"))
        {
          desc = "a clear, open sky stretching above";
        }
        else if (weather.defName.Contains("Fog"))
        {
          desc = "a thick fog rolling past the windows";
        }
        else
        {
          desc = "clouds drifting slowly past the glass";
        }
      }
      yield return new Rule_String("WEATHER_description", desc);
    }

    for (int i = 0; i < participantsData.Count; i++)
    {
      var data = participantsData[i];

      foreach (Rule rule in data.GetRules("ANYPAWN"))
      {
        yield return rule;
      }

      string prefix = i switch
      {
        0 => "GAZER",
        1 => "COMPANION",
        2 => "THIRDPAWN",
        3 => "FOURTHPAWN",
        _ => $"PAWN{i}"
      };

      foreach (Rule rule in data.GetRules(prefix, outConstants))
      {
        yield return rule;
      }
    }
  }

  public override void GenerateTestData()
  {
    base.GenerateTestData();
    participantsData.Clear();
    participantsData.Add(TaleData_Pawn.GenerateRandom());
    participantsData.Add(TaleData_Pawn.GenerateRandom());
    participantsData.Add(TaleData_Pawn.GenerateRandom());
    participantsData.Add(TaleData_Pawn.GenerateRandom());
    weather = WeatherDefOf.Clear;
  }
}
