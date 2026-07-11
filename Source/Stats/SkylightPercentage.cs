using Verse;
using UnityEngine;

using SolarWeb.Stratum.MapComponents;

namespace SolarWeb.Stratum.Stats;

public class SkylightPercentage : RoomStatWorker
{
  public override float GetScore(Room room)
  {
    if (room.PsychologicallyOutdoors || room.CellCount == 0)
    {
      return 0f;
    }

    float glassCount = 0f;
    int totalCells = room.CellCount;
    var map = room.Map;
    var skylightDirt = map.GetComponent<SkylightCoating>();

    foreach (var cell in room.Cells)
    {
      var roof = map.roofGrid.RoofAt(cell);
      if (roof != null && RoofStatCache.IsSkylight(roof))
      {
        float opacity = skylightDirt != null ? skylightDirt.GetCoatingOpacity(cell) : 0f;
        glassCount += (1f - opacity);
      }
    }

    return glassCount / totalCells;
  }
}
