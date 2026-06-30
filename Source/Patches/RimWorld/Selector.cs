using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using Verse;
using Verse.Sound;

using SolarWeb.Stratum.Stats;
using SolarWeb.Stratum.UI;
using SolarWeb.Stratum.WorldComponents;

namespace SolarWeb.Stratum.Patches;

[HarmonyPatch]
public static class Selector_Patch
{
  private static IEnumerable<object> YieldRoofAndOriginal(object roofObj, IEnumerable<object> original)
  {
    yield return roofObj;
    foreach (var obj in original)
    {
      yield return obj;
    }
  }

  [HarmonyPatch(typeof(Selector), "SelectableObjectsUnderMouse")]
  [HarmonyPostfix]
  public static void SelectableObjectsUnderMouse_Postfix(ref IEnumerable<object> __result)
  {
    if (__result == null) return;
    if (!Find.PlaySettings.showRoofOverlay) return;

    Map map = Find.CurrentMap;
    if (map == null || map.roofGrid == null) return;

    IntVec3 cell = Verse.UI.MouseCell();
    if (!cell.InBounds(map)) return;

    RoofDef roof = map.roofGrid.RoofAt(cell);
    if (!RoofStatCache.IsCustomRoof(roof)) return;

    var pool = Find.World?.GetComponent<RoofSelectionPool>();
    if (pool == null) return;

    var roofObj = pool.Get(map, cell, roof);
    __result = YieldRoofAndOriginal(roofObj, __result);
  }

  [HarmonyPatch(typeof(Selector), "SelectableObjectsAt")]
  [HarmonyPostfix]
  public static void SelectableObjectsAt_Postfix(ref IEnumerable<object> __result, IntVec3 c, Map map)
  {
    if (__result == null) return;
    if (!Find.PlaySettings.showRoofOverlay) return;
    if (map == null || map.roofGrid == null) return;
    if (!c.InBounds(map)) return;

    RoofDef roof = map.roofGrid.RoofAt(c);
    if (!RoofStatCache.IsCustomRoof(roof)) return;

    var pool = Find.World?.GetComponent<RoofSelectionPool>();
    if (pool == null) return;

    var roofObj = pool.Get(map, c, roof);
    __result = YieldRoofAndOriginal(roofObj, __result);
  }

  [HarmonyPatch(typeof(Selector), "SelectInternal")]
  [HarmonyPrefix]
  public static bool SelectInternal_Prefix(object obj, bool playSound, bool forceDesignatorDeselect, Selector __instance)
  {
    if (obj is SelectedRoof sr)
    {
      if (!Find.PlaySettings.showRoofOverlay) return false;

      if (__instance.SelectedZone != null || __instance.SelectedPlan != null)
      {
        __instance.ClearSelection();
      }

      if (forceDesignatorDeselect)
      {
        Find.DesignatorManager.Deselect();
      }

      if (__instance.SelectedObjects != null && __instance.SelectedObjects.Count < 200 && !__instance.IsSelected(obj))
      {
        if (sr.map != Find.CurrentMap)
        {
          Current.Game.CurrentMap = sr.map;
          SoundDefOf.MapSelected.PlayOneShotOnCamera();
          Find.CameraDriver.JumpToCurrentMapLoc(sr.cell);
        }

        if (playSound)
        {
          SoundDefOf.ThingSelected.PlayOneShotOnCamera();
        }

        __instance.SelectedObjects.Add(obj);
        SelectionDrawer.Notify_Selected(obj);
      }
      return false;
    }
    return true;
  }

  [HarmonyPatch(typeof(Selector), "DeselectInternal")]
  [HarmonyPostfix]
  public static void DeselectInternal_Postfix(object obj)
  {
    if (obj is SelectedRoof sr)
    {
      sr.Dispose();
    }
  }

  [HarmonyPatch(typeof(Selector), "ClearSelection")]
  [HarmonyPrefix]
  public static void ClearSelection_Prefix(Selector __instance)
  {
    if (__instance.SelectedObjects == null) return;

    foreach (var obj in __instance.SelectedObjects)
    {
      if (obj is SelectedRoof sr)
      {
        sr.Dispose();
      }
    }
  }
}
