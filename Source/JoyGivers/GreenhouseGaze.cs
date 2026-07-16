using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

using SolarWeb.Stratum.Stats;

namespace SolarWeb.Stratum.JoyGivers;

public class GreenhouseGaze : JoyGiver
{
  public override Job TryGiveJob(Pawn pawn)
  {
    if (pawn.Map == null) return null!;

    if (pawn.story?.traits?.HasTrait(TraitDefOf.Undergrounder) == true)
    {
      return null!;
    }

    var map = pawn.Map;
    List<Room> candidateRooms = [];

    var rooms = map.regionGrid.AllRooms;
    foreach (var room in rooms)
    {
      if (room.Role == DefOf.RoomRoleDefOf.Greenhouse && HasPlants(room))
      {
        candidateRooms.Add(room);
      }
    }

    if (candidateRooms.Count == 0)
    {
      return null!;
    }

    foreach (var p in map.mapPawns.AllPawnsSpawned)
    {
      if (p != pawn && p.CurJob?.def == def.jobDef)
      {
        var room = p.GetRoom();
        if (room != null && room.Role == DefOf.RoomRoleDefOf.Greenhouse)
        {
          var gazerPos = p.Position;
          List<IntVec3> adjCandidates = [];
          List<IntVec3> adjFallback = [];

          for (int i = 0; i < 4; i++)
          {
            var adjCell = gazerPos + GenAdj.CardinalDirections[i];
            if (adjCell.InBounds(map) &&
                adjCell.Walkable(map) &&
                !adjCell.IsForbidden(pawn) &&
                pawn.CanReach(adjCell, PathEndMode.OnCell, Danger.None) &&
                adjCell.GetRoom(map) == room &&
                !IsCellOccupied(adjCell, map))
            {
              var roof = map.roofGrid.RoofAt(adjCell);
              if (roof != null && RoofStatCache.IsSkylight(roof))
              {
                adjCandidates.Add(adjCell);
              }
              else
              {
                adjFallback.Add(adjCell);
              }
            }
          }

          IntVec3 targetAdjCell = IntVec3.Invalid;
          if (adjCandidates.Count > 0)
          {
            targetAdjCell = adjCandidates.RandomElement();
          }
          else if (adjFallback.Count > 0)
          {
            targetAdjCell = adjFallback.RandomElement();
          }

          if (targetAdjCell.IsValid)
          {
            return JobMaker.MakeJob(def.jobDef, targetAdjCell);
          }
        }
      }
    }

    var targetRoom = candidateRooms.RandomElementByWeight(r => r.CellCount);
    if (targetRoom == null) return null!;

    List<IntVec3> candidateCells = [];
    List<IntVec3> fallbackCells = [];

    foreach (var cell in targetRoom.Cells)
    {
      if (cell.Walkable(map) &&
          !cell.IsForbidden(pawn) &&
          pawn.CanReach(cell, PathEndMode.OnCell, Danger.None) &&
          !IsCellOccupied(cell, map))
      {
        var roof = map.roofGrid.RoofAt(cell);
        if (roof != null && RoofStatCache.IsSkylight(roof))
        {
          candidateCells.Add(cell);
        }
        else
        {
          fallbackCells.Add(cell);
        }
      }
    }

    IntVec3 targetCell = IntVec3.Invalid;
    if (candidateCells.Count > 0)
    {
      targetCell = candidateCells.RandomElement();
    }
    else if (fallbackCells.Count > 0)
    {
      targetCell = fallbackCells.RandomElement();
    }

    if (!targetCell.IsValid)
    {
      return null!;
    }

    return JobMaker.MakeJob(def.jobDef, targetCell);
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

  private static bool IsCellOccupied(IntVec3 cell, Map map)
  {
    List<Thing> things = cell.GetThingList(map);
    for (int i = 0; i < things.Count; i++)
    {
      if (things[i] is Building or Plant)
      {
        return true;
      }
    }
    return false;
  }
}
