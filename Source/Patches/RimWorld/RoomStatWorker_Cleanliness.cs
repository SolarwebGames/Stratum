using HarmonyLib;
using RimWorld;
using SolarWeb.Stratum.Stats;
using Verse;

namespace SolarWeb.Stratum.Patches;

[HarmonyPatch]
public static class RoomStatWorker_Cleanliness_Patch
{
  [HarmonyPatch(typeof(RoomStatWorker_Cleanliness), nameof(RoomStatWorker_Cleanliness.GetScore))]
  [HarmonyPostfix]
  public static void GetScore_Postfix(Room room, ref float __result)
  {
    float totalCleanliness = 0f;
    var map = room.Map;
    if (map == null) return;

    foreach (var cell in room.Cells)
    {
      var roof = map.roofGrid.RoofAt(cell);
      if (roof != null && RoofStatCache.IsCustomRoof(roof))
      {
        totalCleanliness += RoofStatCache.GetCleanliness(roof);
      }
    }

    __result += totalCleanliness / room.CellCount;
  }
}
