using HarmonyLib;
using SolarWeb.Stratum.MapComponents;
using SolarWeb.Stratum.Stats;
using Verse;

namespace SolarWeb.Stratum.Patches;

[HarmonyPatch]
public static class RoofGrid_Patch
{
  [HarmonyPatch(typeof(RoofGrid), nameof(RoofGrid.GetCellBool))]
  [HarmonyPrefix]
  public static bool GetCellBool_Prefix(RoofGrid __instance, int index, ref bool __result)
  {
    var roof = __instance.RoofAt(index);
    if (RoofStatCache.IsCustomRoof(roof))
    {
      __result = false;
      return false;
    }
    return true;
  }

  [HarmonyPatch(typeof(RoofGrid), nameof(RoofGrid.Roofed), [typeof(IntVec3)])]
  [HarmonyPrefix]
  public static bool Roofed_Prefix(RoofGrid __instance, IntVec3 c, ref bool __result)
  {
    var roof = __instance.RoofAt(c);
    if (roof != null && RoofStatCache.IsCustomRoof(roof))
    {
      __result = true;
      return false;
    }
    return true;
  }

  [HarmonyPatch(typeof(RoofGrid), nameof(RoofGrid.SetRoof))]
  [HarmonyPrefix]
  public static void SetRoof_Prefix(IntVec3 c, Map ___map, out RoofDef? __state)
  {
    if (___map == null || ___map.roofGrid == null)
    {
      __state = null;
      return;
    }
    __state = ___map.roofGrid.RoofAt(c);
  }

  [HarmonyPatch(typeof(RoofGrid), nameof(RoofGrid.SetRoof))]
  [HarmonyPostfix]
  public static void SetRoof_Postfix(IntVec3 c, RoofDef def, Map ___map, RoofDef? __state)
  {
    if (___map == null || ___map.roofGrid == null || ___map.areaManager == null) return;
    var currentRoof = ___map.roofGrid.RoofAt(c);
    if (currentRoof == __state) return;

    Utilities.StratumHooks.Notify_RoofChanged(___map, c, __state, currentRoof);
    
    if (___map.areaManager.NoRoof != null)
    {
      ___map.areaManager.NoRoof[c] = false;
    }
    
    if (___map.areaManager.BuildRoof != null)
    {
      ___map.areaManager.BuildRoof[c] = false;
    }

    if (___map.regionAndRoomUpdater != null && ___map.regionAndRoomUpdater.Enabled)
    {
      var room = c.GetRoom(___map);
      if (room != null)
      {
        foreach (var district in room.Districts)
        {
          district.Notify_RoofChanged();
        }
      }
    }

    var integrity = ___map.GetComponent<RoofIntegrityGrid>();
    if (def != null && RoofStatCache.IsCustomRoof(def))
    {
      ThingDef? stuff = integrity?.GetStuff(c);
      UnityEngine.Color? tint = null;
      if (DebugSettings.godMode)
      {
        var designator = Find.DesignatorManager.SelectedDesignator as AI.Designators.BuildCustomRoof;
        if (designator != null)
        {
          stuff = designator.StuffDef;
          tint = designator.SelectedTint;
        }
      }

      if (stuff == null && def.isNatural)
      {
        stuff = RoofIntegrityGrid.GetStonyStuffForCell(def, c, ___map);
      }

      if (stuff == null && GravshipPlacementUtility_SpawnRoofs_Patch.CurrentLandingGravship != null)
      {
        var local = c - GravshipPlacementUtility_SpawnRoofs_Patch.CurrentLandingRoot;
        if (GravshipPlacementUtility_SpawnRoofs_Patch.CurrentRoofData != null &&
            GravshipPlacementUtility_SpawnRoofs_Patch.CurrentRoofData.TryGetValue(local, out var cellData))
        {
          stuff = cellData.stuff;
          integrity?.InitializeRoof(c, def, stuff, cellData.glassTint, cellData.hitPoints);
        }
        else
        {
          integrity?.InitializeRoof(c, def, stuff, tint);
        }
      }
      else
      {
        integrity?.InitializeRoof(c, def, stuff, tint);
      }
    }
    else
    {
      integrity?.RemoveRoof(c);
    }

    if (Find.Selector.SelectedObjects.Count > 0)
    {
      for (int i = Find.Selector.SelectedObjects.Count - 1; i >= 0; i--)
      {
        if (Find.Selector.SelectedObjects[i] is UI.SelectedRoof sr && sr.map == ___map && sr.cell == c)
        {
          if (def == null || sr.def != def)
          {
            Find.Selector.Deselect(sr);
          }
        }
      }
    }
  }
}
