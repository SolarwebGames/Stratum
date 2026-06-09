using Verse;

namespace SolarWeb.Stratum.Stats;

public class SkylightPercentage : RoomStatWorker
{
  public override float GetScore(Room room)
  {
    if (room.PsychologicallyOutdoors || room.CellCount == 0)
    {
      return 0f;
    }

    int glassCount = 0;
    int totalCells = room.CellCount;
    var map = room.Map;

    foreach (var cell in room.Cells)
    {
      var roof = map.roofGrid.RoofAt(cell);
      if (roof != null && RoofStatCache.IsSkylight(roof))
      {
        glassCount++;
      }
    }

    return (float)glassCount / totalCells;
  }
}
