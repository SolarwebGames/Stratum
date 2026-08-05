using HarmonyLib;
using Verse;

using SolarWeb.Stratum.Stats;
using SolarWeb.Stratum.Hooks;
using SolarWeb.Stratum.MapComponents;

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
  public static bool SetRoof_Prefix(IntVec3 c, ref RoofDef def, Map ___map, out RoofDef? __state)
  {
    if (___map == null || ___map.roofGrid == null)
    {
      __state = null;
      return true;
    }

    var oldRoof = ___map.roofGrid.RoofAt(c);
    __state = oldRoof;

    {
      bool allow = true;
      RoofDef? newRoof = def;
      MapHookRegistry.InvokeBeforeSetRoof(___map, c, oldRoof, ref newRoof, ref allow);
      def = newRoof!;
      if (!allow)
      {
        return false;
      }
    }
    return true;
  }

  [HarmonyPatch(typeof(RoofGrid), nameof(RoofGrid.SetRoof))]
  [HarmonyPostfix]
  public static void SetRoof_Postfix(IntVec3 c, Map ___map, RoofDef? __state)
  {
    if (___map == null || ___map.roofGrid == null) return;
    var currentRoof = ___map.roofGrid.RoofAt(c);
    if (currentRoof == __state) return;

    MapHookRegistry.NotifyRoofChanged(___map, c, __state, currentRoof);

    if (___map.areaManager.NoRoof != null)
    {
      ___map.areaManager.NoRoof[c] = false;
    }
    if (___map.areaManager.BuildRoof != null)
    {
      ___map.areaManager.BuildRoof[c] = false;
    }

    var region = ___map.regionGrid?.GetValidRegionAt_NoRebuild(c);
    region?.District?.Notify_RoofChanged();

    var integrity = ___map.GetComponent<RoofIntegrityGrid>();
    if (currentRoof != null && RoofStatCache.IsCustomRoof(currentRoof))
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

      if (stuff == null && currentRoof.isNatural)
      {
        stuff = RoofIntegrityGrid.GetStonyStuffForCell(currentRoof, c, ___map);
      }

      if (stuff == null && GravshipPlacementUtility_SpawnRoofs_Patch.CurrentLandingGravship != null)
      {
        var local = c - GravshipPlacementUtility_SpawnRoofs_Patch.CurrentLandingRoot;
        if (GravshipPlacementUtility_SpawnRoofs_Patch.CurrentRoofData != null &&
            GravshipPlacementUtility_SpawnRoofs_Patch.CurrentRoofData.TryGetValue(local, out var cellData))
        {
          stuff = cellData.stuff;
          integrity?.InitializeRoof(c, currentRoof, stuff, cellData.glassTint, cellData.hitPoints);
        }
        else
        {
          integrity?.InitializeRoof(c, currentRoof, stuff, tint);
        }
      }
      else
      {
        integrity?.InitializeRoof(c, currentRoof, stuff, tint);
      }
    }
    else
    {
      integrity?.RemoveRoof(c);
    }

    if (Find.Selector != null && Find.Selector.SelectedObjects != null && Find.Selector.SelectedObjects.Count > 0)
    {
      for (int i = Find.Selector.SelectedObjects.Count - 1; i >= 0; i--)
      {
        if (Find.Selector.SelectedObjects[i] is UI.SelectedRoof sr && sr.map == ___map && sr.cell == c)
        {
          if (currentRoof == null || sr.def != currentRoof)
          {
            Find.Selector.Deselect(sr);
          }
        }
      }
    }
  }
}
