using System.Collections.Generic;
using RimWorld;
using Verse;

namespace SolarWeb.Stratum.MapComponents;

public class ThermostatTracker(Map map) : MapComponent(map)
{
  private readonly HashSet<Things.Thermostat> thermostats = [];

  public void Register(Things.Thermostat t)
  {
    thermostats.Add(t);
  }

  public void Deregister(Things.Thermostat t)
  {
    thermostats.Remove(t);
  }

  public bool TryGetLowestTargetTemperature(Room room, out float targetTemp)
  {
    targetTemp = float.MaxValue;
    bool found = false;

    foreach (var t in thermostats)
    {
      if (t.GetRoom() == room)
      {
        var tempComp = t.GetComp<CompTempControl>();
        if (tempComp != null)
        {
          var powerComp = t.GetComp<CompPowerTrader>();
          if (powerComp == null || powerComp.PowerOn)
          {
            found = true;
            if (tempComp.targetTemperature < targetTemp)
            {
              targetTemp = tempComp.targetTemperature;
            }
          }
        }
      }
    }

    return found;
  }

  public int GetNetworkOffsetForRoom(Room room)
  {
    foreach (var t in thermostats)
    {
      if (t.GetRoom() == room)
      {
        var powerComp = t.GetComp<CompPowerTrader>();
        if (powerComp == null || powerComp.PowerOn)
        {
          return t.networkOffset;
        }
      }
    }
    return 0;
  }
}
