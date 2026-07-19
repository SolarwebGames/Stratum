using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using Verse;
using Verse.Sound;

using SolarWeb.Stratum.Stats;
using SolarWeb.Stratum.UI;
using SolarWeb.Stratum.WorldComponents;
using SolarWeb.Stratum.Utilities;
using SolarWeb.Stratum.DefModExtensions;

namespace SolarWeb.Stratum.Patches;

[HarmonyPatch(typeof(Selector))]
public static class Selector_Patch
{
  private static IEnumerable<object> YieldRoofAndOriginal(object roofObj, IEnumerable<object> original)
  {
    var rooftopBuildings = new List<object>();
    var hangingBuildings = new List<object>();
    var otherObjects = new List<object>();

    foreach (var obj in original)
    {
      if (obj is Thing t && RoofBuildings.IsRoofBuildingOrBlueprintOrFrame(t))
      {
        if (RoofBuildings.GetAttachmentType(t) == RoofAttachmentType.Rooftop)
        {
          rooftopBuildings.Add(obj);
        }
        else
        {
          hangingBuildings.Add(obj);
        }
      }
      else
      {
        otherObjects.Add(obj);
      }
    }

    foreach (var obj in rooftopBuildings)
    {
      yield return obj;
    }

    yield return roofObj;

    foreach (var obj in hangingBuildings)
    {
      yield return obj;
    }

    foreach (var obj in otherObjects)
    {
      yield return obj;
    }
  }

  [HarmonyPatch("SelectableObjectsUnderMouse")]
  [HarmonyPostfix]
  public static void SelectableObjectsUnderMouse_Postfix(ref IEnumerable<object> __result)
  {
    if (__result == null) return;
    if (!Find.PlaySettings.showRoofOverlay) return;

    if (Find.ColonistBar != null)
    {
      UnityEngine.Vector2 mousePos = Verse.UI.MousePositionOnUIInverted;
      if (Find.ColonistBar.ColonistOrCorpseAt(mousePos) != null || Find.ColonistBar.CaravanMemberCaravanAt(mousePos) != null)
      {
        return;
      }
    }

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

  [HarmonyPatch("SelectableObjectsAt")]
  [HarmonyPostfix]
  public static void SelectableObjectsAt_Postfix(ref IEnumerable<object> __result, IntVec3 c, Map map)
  {
    if (__result == null) return;
    if (!Find.PlaySettings.showRoofOverlay) return;
    if (map == null || map.roofGrid == null) return;
    if (!c.InBounds(map)) return;
    if (!c.Fogged(map)) return;

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

  [HarmonyPatch("DeselectInternal")]
  [HarmonyPostfix]
  public static void DeselectInternal_Postfix(object obj)
  {
    if (obj is SelectedRoof sr)
    {
      sr.Dispose();
    }
  }

  [HarmonyPatch("ClearSelection")]
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
