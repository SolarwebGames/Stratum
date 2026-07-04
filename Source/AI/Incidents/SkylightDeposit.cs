using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

using SolarWeb.Stratum.MapComponents;

namespace SolarWeb.Stratum.AI.Incidents;

public class SkylightDeposit : IncidentWorker
{
  protected override bool CanFireNowSub(IncidentParms parms)
  {
    Map map = (Map)parms.target;
    if (map == null) return false;

    var dirt = map.GetComponent<SkylightCoating>();
    if (dirt == null) return false;

    return TryFindCandidateRoom(map, out _);
  }

  protected override bool TryExecuteWorker(IncidentParms parms)
  {
    Map map = (Map)parms.target;
    if (map == null) return false;

    var dirt = map.GetComponent<SkylightCoating>();
    if (dirt == null) return false;

    if (!TryFindCandidateRoom(map, out Room room)) return false;

    Season season = GenLocalDate.Season(map);
    bool isPollen = (season == Season.Spring || season == Season.Summer);

    Color dirtColor = isPollen ? new Color(0.3f, 0.28f, 0.1f) : new Color(0.22f, 0.18f, 0.13f);
    IntVec3 firstCell = IntVec3.Invalid;

    foreach (var cell in room.Cells)
    {
      var roof = map.roofGrid.RoofAt(cell);
      if (roof != null && Stats.RoofStatCache.IsSkylight(roof))
      {
        dirt.SetDirtLevel(cell, 1.0f, dirtColor);
        
        if (!firstCell.IsValid) firstCell = cell;

        FilthMaker.TryMakeFilth(cell, map, ThingDefOf.Filth_Dirt, 1);
      }
    }

    if (firstCell.IsValid)
    {
      string roomLabel = room.Role?.label ?? "room";
      string label = isPollen 
        ? "SolarWeb_Stratum_PollenCoating_Label".Translate() 
        : "SolarWeb_Stratum_DustCoating_Label".Translate();
      string text = isPollen 
        ? "SolarWeb_Stratum_PollenCoating_Description".Translate(roomLabel)
        : "SolarWeb_Stratum_DustCoating_Description".Translate(roomLabel);

      SendStandardLetter(label, text, LetterDefOf.NegativeEvent, parms, new TargetInfo(firstCell, map));
      return true;
    }

    return false;
  }

  private bool TryFindCandidateRoom(Map map, out Room room)
  {
    room = null!;
    var rooms = map.regionGrid.AllRooms.Where(r => !r.PsychologicallyOutdoors && r.CellCount > 0);
    
    List<Room> candidates = [];
    foreach (var r in rooms)
    {
      bool hasSkylight = false;
      foreach (var cell in r.Cells)
      {
        var roof = map.roofGrid.RoofAt(cell);
        if (roof != null && Stats.RoofStatCache.IsSkylight(roof))
        {
          hasSkylight = true;
          break;
        }
      }
      if (hasSkylight)
      {
        candidates.Add(r);
      }
    }

    if (candidates.Count > 0)
    {
      room = candidates.RandomElement();
      return true;
    }

    return false;
  }
}
