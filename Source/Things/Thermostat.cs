using Verse;
using System.Collections.Generic;
using RimWorld;
using UnityEngine;

namespace SolarWeb.Stratum.Things;

public class Thermostat : Building
{
  public int networkOffset = 0;

  public override void ExposeData()
  {
    base.ExposeData();
    Scribe_Values.Look(ref networkOffset, "networkOffset", 0);
  }

  public override IEnumerable<Gizmo> GetGizmos()
  {
    foreach (var gizmo in base.GetGizmos())
    {
      yield return gizmo;
    }

    yield return new Command_Action
    {
      action = delegate
      {
        networkOffset++;
      },
      defaultLabel = "Stratum_ReconnectRoofNet".Translate(),
      defaultDesc = "Stratum_ReconnectRoofNetDesc".Translate(),
      icon = ContentFinder<Texture2D>.Get("UI/Commands/TryReconnect", true),
      hotKey = KeyBindingDefOf.Misc1
    };
  }
  public override void SpawnSetup(Map map, bool respawningAfterLoad)
  {
    base.SpawnSetup(map, respawningAfterLoad);
    map.GetComponent<MapComponents.ThermostatTracker>()?.Register(this);
  }

  public override void DeSpawn(DestroyMode mode = DestroyMode.Vanish)
  {
    Map?.GetComponent<MapComponents.ThermostatTracker>()?.Deregister(this);
    base.DeSpawn(mode);
  }

  public override string GetInspectString()
  {
    var sb = new System.Text.StringBuilder();
    string baseString = base.GetInspectString();
    if (!string.IsNullOrEmpty(baseString))
    {
      sb.AppendLine(baseString);
    }

    var radiatorManager = Map?.GetComponent<MapComponents.ActiveRadiatorManager>();
    var room = this.GetRoom();

    if (radiatorManager != null && room != null && !room.UsesOutdoorTemperature)
    {
      if (radiatorManager.TryGetRoomStats(room, out int totalTiles, out int poweredTiles, out bool cooling, out float powerDraw))
      {
        if (totalTiles > 0)
        {
          sb.Append($"Connected Radiators: {totalTiles} tiles");
          if (cooling)
          {
            sb.Append($" ({poweredTiles} actively cooling, {powerDraw:F0}W)");
          }
          else
          {
            sb.Append($" (standby, {powerDraw:F0}W)");
          }
        }
        else
        {
          sb.Append("Connected Radiators: None found in room");
        }
      }
      else
      {
        sb.Append("Connected Radiators: Scanning...");
      }
    }

    return sb.ToString().TrimEndNewlines();
  }
}
