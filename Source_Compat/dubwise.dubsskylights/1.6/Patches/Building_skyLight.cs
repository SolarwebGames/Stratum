using HarmonyLib;
using Dubs_Skylight;
using Verse;
using UnityEngine;

using SolarWeb.Stratum.MapComponents;
using SolarWeb.Stratum.Stats;

namespace SolarWeb.Stratum.DubsSkylights.Patches;

[HarmonyPatch(typeof(Building_skyLight))]
public static class Building_skyLight_Patch
{
  [HarmonyPatch(nameof(Building_skyLight.SpawnSetup))]
  [HarmonyPostfix]
  public static void SpawnSetup_Postfix(Building_skyLight __instance, Map map)
  {
    if (map == null) return;
    var integrity = map.GetComponent<RoofIntegrityGrid>();
    if (integrity == null) return;

    CellRect rect = __instance.OccupiedRect();
    foreach (IntVec3 cell in rect)
    {
      if (!cell.InBounds(map)) continue;
      var roof = map.roofGrid.RoofAt(cell);
      if (roof == null || !RoofStatCache.IsSkylight(roof)) continue;

      int oldMaxHP = RoofStatCache.GetMaxHitPoints(roof, integrity.GetStuff(cell));
      int newMaxHP = integrity.GetMaxHitPoints(cell);

      if (oldMaxHP > 0 && newMaxHP > 0 && newMaxHP != oldMaxHP)
      {
        int oldHP = integrity.GetHitPoints(cell);
        int newHP = Mathf.RoundToInt((float)oldHP * newMaxHP / oldMaxHP);
        integrity.SetHitPoints(cell, (short)newHP);
      }
    }
  }

  [HarmonyPatch(nameof(Building_skyLight.DeSpawn))]
  [HarmonyPrefix]
  public static void DeSpawn_Prefix(Building_skyLight __instance)
  {
    var map = __instance.Map;
    if (map == null) return;
    var integrity = map.GetComponent<RoofIntegrityGrid>();
    if (integrity == null) return;

    CellRect rect = __instance.OccupiedRect();
    foreach (IntVec3 cell in rect)
    {
      if (!cell.InBounds(map)) continue;
      var roof = map.roofGrid.RoofAt(cell);
      if (roof == null || !RoofStatCache.IsSkylight(roof)) continue;

      int oldMaxHP = integrity.GetMaxHitPoints(cell);
      int newMaxHP = RoofStatCache.GetMaxHitPoints(roof, integrity.GetStuff(cell));

      if (oldMaxHP > 0 && newMaxHP > 0 && newMaxHP != oldMaxHP)
      {
        int oldHP = integrity.GetHitPoints(cell);
        int newHP = Mathf.RoundToInt((float)oldHP * newMaxHP / oldMaxHP);
        integrity.SetHitPoints(cell, (short)newHP);
      }
    }
  }
}
