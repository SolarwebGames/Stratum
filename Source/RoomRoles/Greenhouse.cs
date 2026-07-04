using RimWorld;
using Verse;

using SolarWeb.Stratum.Stats;

namespace SolarWeb.Stratum.RoomRoles;

public class Greenhouse : RoomRoleWorker
{
  public override float GetScore(Room room)
  {
    if (room.PsychologicallyOutdoors || room.CellCount == 0)
    {
      return 0f;
    }

    var map = room.Map;
    if (map == null)
    {
      return 0f;
    }

    int skylightCount = 0;
    int growingZoneCellCount = 0;

    foreach (var cell in room.Cells)
    {
      var roof = map.roofGrid.RoofAt(cell);
      if (roof != null && RoofStatCache.IsSkylight(roof))
      {
        skylightCount++;
      }

      var zone = map.zoneManager.ZoneAt(cell);
      if (zone is Zone_Growing)
      {
        growingZoneCellCount++;
      }
    }

    int hydroponicsCellCount = 0;
    var containedThings = room.ContainedAndAdjacentThings;
    for (int i = 0; i < containedThings.Count; i++)
    {
      if (containedThings[i] is Building_PlantGrower plantGrower)
      {
        foreach (var cell in plantGrower.OccupiedRect())
        {
          if (cell.GetRoom(map) == room)
          {
            hydroponicsCellCount++;
          }
        }
      }
    }

    if (skylightCount == 0 || (growingZoneCellCount == 0 && hydroponicsCellCount == 0))
    {
      return 0f;
    }

    return 20f + (growingZoneCellCount + hydroponicsCellCount) * 2f + skylightCount * 1.5f;
  }

  public override float GetScoreDeltaIfBuildingPlaced(Room room, ThingDef buildingDef)
  {
    if (buildingDef.building == null || buildingDef.thingClass == null || !typeof(Building_PlantGrower).IsAssignableFrom(buildingDef.thingClass))
    {
      return 0f;
    }

    var map = room.Map;
    if (map == null)
    {
      return 0f;
    }

    bool hasSkylight = false;
    foreach (var cell in room.Cells)
    {
      var roof = map.roofGrid.RoofAt(cell);
      if (roof != null && RoofStatCache.IsSkylight(roof))
      {
        hasSkylight = true;
        break;
      }
    }

    if (!hasSkylight)
    {
      return 0f;
    }

    float currentScore = GetScore(room);
    if (currentScore > 0f)
    {
      return buildingDef.size.Area * 2f;
    }

    int skylightCount = 0;
    foreach (var cell in room.Cells)
    {
      var roof = map.roofGrid.RoofAt(cell);
      if (roof != null && RoofStatCache.IsSkylight(roof))
      {
        skylightCount++;
      }
    }

    return 20f + buildingDef.size.Area * 2f + skylightCount * 1.5f;
  }
}
