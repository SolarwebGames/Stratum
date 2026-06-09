using HarmonyLib;
using RimWorld;
using SolarWeb.Stratum.Stats;
using SolarWeb.Stratum.MapComponents;
using Verse;

namespace SolarWeb.Stratum.Patches;

[HarmonyPatch]
public static class RoomStatWorker_Wealth_Patch
{
  [HarmonyPatch(typeof(RoomStatWorker_Wealth), nameof(RoomStatWorker_Wealth.GetScore))]
  [HarmonyPostfix]
  public static void GetScore_Postfix(Room room, ref float __result)
  {
    float totalWealth = 0f;
    var map = room.Map;
    if (map == null) return;

    var grid = map.GetComponent<RoofIntegrityGrid>();
    foreach (var cell in room.Cells)
    {
      var roof = map.roofGrid.RoofAt(cell);
      if (roof != null && RoofStatCache.IsCustomRoof(roof))
      {
        var stuff = grid?.GetStuff(cell);
        totalWealth += RoofStatCache.GetWealth(roof, stuff);
      }
    }

    __result += totalWealth;
  }
}
