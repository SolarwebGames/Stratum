using RimWorld;
using Verse;

namespace SolarWeb.Stratum.ThingComps;

public class RotaryTurbineVent : ThingComp
{
  private CompProperties.RotaryTurbineVent? cachedProps;

  public CompProperties.RotaryTurbineVent? Props =>
    cachedProps ??= props as CompProperties.RotaryTurbineVent;

  public override void CompTickRare()
  {
    base.CompTickRare();
    if (parent == null || !parent.Spawned || parent.Map == null) return;

    Room room = parent.Position.GetRoom(parent.Map);
    if (room == null || room.UsesOutdoorTemperature) return;

    float outdoorTemp = parent.Map.mapTemperature.OutdoorTemp;
    float roomTemp = room.Temperature;

    if (roomTemp > outdoorTemp)
    {
      float baseCooling = Props != null ? Props.baseCoolingCapacity : 12f;
      float windSpeed = parent.Map.windManager.WindSpeed;
      float cooling = baseCooling * windSpeed;
      float actualHeat = -cooling * 4.1666665f;

      float tempChange = actualHeat / (room.CellCount * 1.2f);
      if (roomTemp + tempChange < outdoorTemp)
      {
        actualHeat = (outdoorTemp - roomTemp) * (room.CellCount * 1.2f);
      }

      GenTemperature.PushHeat(parent.Position, parent.Map, actualHeat);
    }
  }

  public override string CompInspectStringExtra()
  {
    if (parent == null || !parent.Spawned || parent.Map == null) return base.CompInspectStringExtra();

    var sb = new System.Text.StringBuilder();
    string baseExtra = base.CompInspectStringExtra();
    if (!string.IsNullOrEmpty(baseExtra))
    {
      sb.AppendLine(baseExtra);
    }

    float windSpeed = parent.Map.windManager.WindSpeed;
    sb.AppendLine("SolarWeb_Stratum_WindSpeed".Translate(windSpeed.ToString("F2")));

    Room room = parent.Position.GetRoom(parent.Map);
    if (room != null && !room.UsesOutdoorTemperature)
    {
      float outdoorTemp = parent.Map.mapTemperature.OutdoorTemp;
      float roomTemp = room.Temperature;
      if (roomTemp > outdoorTemp)
      {
        float baseCooling = Props != null ? Props.baseCoolingCapacity : 12f;
        float cooling = baseCooling * windSpeed;
        sb.Append("SolarWeb_Stratum_RotaryTurbineVent_CoolingRate".Translate(cooling.ToString("F1")));
      }
      else
      {
        sb.Append("SolarWeb_Stratum_RotaryTurbineVent_NoCooling".Translate());
      }
    }
    else
    {
      sb.Append("SolarWeb_Stratum_RotaryTurbineVent_NoRoom".Translate());
    }

    return sb.ToString().TrimEndNewlines();
  }
}
