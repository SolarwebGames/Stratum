using System.Runtime.CompilerServices;
using HarmonyLib;
using Verse;

using SolarWeb.Stratum.Stats;
using SolarWeb.Stratum.Hooks;

namespace SolarWeb.Stratum.Patches;

[HarmonyPatch(typeof(RoomTempTracker))]
public static class RoomTempTracker_Patch
{
  private static readonly ConditionalWeakTable<RoomTempTracker, StrongBox<float>> roomConductivity = new();

  // Multiplier to relate our 0.1 standard to RimWorld's 5E-05
  // 0.1 * 0.0005 = 0.00005
  private const float ConductivityToRimWorldRate = 0.0005f;

  private static readonly AccessTools.FieldRef<RoomTempTracker, Room> roomRef = AccessTools.FieldRefAccess<RoomTempTracker, Room>("room");
  private delegate float TempDiffFromOutdoorsAdjustedDelegate(RoomTempTracker instance);
  private static readonly TempDiffFromOutdoorsAdjustedDelegate TempDiffFromOutdoorsAdjusted = 
    AccessTools.MethodDelegate<TempDiffFromOutdoorsAdjustedDelegate>(AccessTools.Method(typeof(RoomTempTracker), "TempDiffFromOutdoorsAdjusted"));

  [HarmonyPatch("CalculateRoofCovereage")]
  [HarmonyPostfix]
  public static void CalculateRoofCovereage_Postfix(RoomTempTracker __instance, Map map, Room ___room)
  {
    if (___room == null || ___room.Cells == null || map == null) return;
    float totalConductivity = 0f;
    int count = 0;

    foreach (var cell in ___room.Cells)
    {
      count++;
      var roof = cell.GetRoof(map);
      if (roof == null) continue;
      if (roof.isThickRoof) continue;

      if (RoofStatCache.IsCustomRoof(roof))
      {
        var integrity = map.GetComponent<MapComponents.RoofIntegrityGrid>();
        float conductivity = RoofStatCache.GetThermalConductivity(roof, integrity?.GetStuff(cell));
        conductivity = MapHookRegistry.GetCellThermalConductivity(map, cell, conductivity);
        totalConductivity += conductivity;
      }
      else
      {
        totalConductivity += 0.1f;
      }
    }

    if (count > 0)
    {
      roomConductivity.Remove(__instance);
      roomConductivity.Add(__instance, new StrongBox<float>(totalConductivity / count));
    }
  }

  [HarmonyPatch("ThinRoofEqualizationTempChangePerInterval")]
  [HarmonyPrefix]
  public static bool ThinRoofEqualizationTempChangePerInterval_Prefix(RoomTempTracker __instance, ref float __result)
  {
    if (roomConductivity.TryGetValue(__instance, out var box))
    {
      float avgConductivity = box.Value;
      if (avgConductivity < 1E-06f)
      {
        __result = 0f;
        return false;
      }

      float tempDiff = TempDiffFromOutdoorsAdjusted(__instance);
      float rate = avgConductivity * ConductivityToRimWorldRate;
      
      __result = tempDiff * rate * 120f;
      return false;
    }
    return true;
  }
}
