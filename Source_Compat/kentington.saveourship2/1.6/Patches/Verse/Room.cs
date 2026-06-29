using HarmonyLib;
using SaveOurShip2;
using Verse;

using SolarWeb.Stratum.Stats;

namespace SolarWeb.Stratum.SOS2.Patches;

[HarmonyAfter("ShipsHaveInsides")]
[HarmonyPatch(typeof(Room))]
public static class Room_OpenRoofCount_Patch
{
  [HarmonyPatch(nameof(Room.OpenRoofCount), MethodType.Getter)]
  [HarmonyPostfix]
  public static void OpenRoofCount_Postfix(Room __instance, ref int __result, ref int ___cachedOpenRoofCount)
  {
    if (__result != 0) return;

    var map = __instance.Map;
    if (map == null || !map.IsSpace()) return;

    var integrity = map.GetComponent<MapComponents.RoofIntegrityGrid>();

    foreach (var cell in __instance.Cells)
    {
      var roof = map.roofGrid.RoofAt(cell);
      if (roof != null && RoofStatCache.IsCustomRoof(roof))
      {
        var stuff = integrity?.GetStuff(cell);
        if (!RoofStatCache.GetIsAirtight(roof, stuff))
        {
          __result = 1;
          ___cachedOpenRoofCount = 1;
          return;
        }
      }
    }
  }
}
