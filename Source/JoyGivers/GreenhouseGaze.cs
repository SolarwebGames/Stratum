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

          while (adjCandidates.Count > 0)
          {
            int index = Rand.Range(0, adjCandidates.Count);
            IntVec3 cell = adjCandidates[index];
            if (pawn.CanReach(cell, PathEndMode.OnCell, Danger.None))
            {
              return JobMaker.MakeJob(def.jobDef, cell);
            }
            adjCandidates.RemoveAt(index);
          }

          while (adjFallback.Count > 0)
          {
            int index = Rand.Range(0, adjFallback.Count);
            IntVec3 cell = adjFallback[index];
            if (pawn.CanReach(cell, PathEndMode.OnCell, Danger.None))
            {
              return JobMaker.MakeJob(def.jobDef, cell);
            }
            adjFallback.RemoveAt(index);
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

    while (candidateCells.Count > 0)
    {
      int index = Rand.Range(0, candidateCells.Count);
      IntVec3 cell = candidateCells[index];
      if (pawn.CanReach(cell, PathEndMode.OnCell, Danger.None))
      {
        return JobMaker.MakeJob(def.jobDef, cell);
      }
      candidateCells.RemoveAt(index);
    }

    while (fallbackCells.Count > 0)
    {
      int index = Rand.Range(0, fallbackCells.Count);
      IntVec3 cell = fallbackCells[index];
      if (pawn.CanReach(cell, PathEndMode.OnCell, Danger.None))
      {
        return JobMaker.MakeJob(def.jobDef, cell);
      }
      fallbackCells.RemoveAt(index);
    }

    return null!;
  }

  private bool HasPlants(Room room)
  {
    var containedThings = room.ContainedAndAdjacentThings;
    for (int i = 0; i < containedThings.Count; i++)
    {
      if (containedThings[i] is Plant)
      {
        return true;
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
