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
  [HarmonyPatch(typeof(Selector), "SelectableObjectsUnderMouse")]
  [HarmonyPostfix]
  public static IEnumerable<object> SelectableObjectsUnderMouse_Postfix(IEnumerable<object> __result)
  {
    if (Find.PlaySettings.showRoofOverlay)
    {
      IntVec3 cell = Verse.UI.MouseCell();
      Map map = Find.CurrentMap;
      if (cell.InBounds(map))
      {
        RoofDef roof = map.roofGrid.RoofAt(cell);
        if (RoofStatCache.IsCustomRoof(roof))
        {
          yield return Find.World.GetComponent<RoofSelectionPool>().Get(map, cell, roof);
        }
      }
    }

    foreach (var obj in __result) yield return obj;
  }

  [HarmonyPatch(typeof(Selector), "SelectableObjectsAt")]
  [HarmonyPostfix]
  public static IEnumerable<object> SelectableObjectsAt_Postfix(IEnumerable<object> __result, IntVec3 c, Map map)
  {
    if (Find.PlaySettings.showRoofOverlay)
    {
      if (c.InBounds(map))
      {
        RoofDef roof = map.roofGrid.RoofAt(c);
        if (RoofStatCache.IsCustomRoof(roof))
        {
          yield return Find.World.GetComponent<RoofSelectionPool>().Get(map, c, roof);
        }
      }
    }

    foreach (var obj in __result) yield return obj;
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

      if (__instance.SelectedObjects.Count < 200 && !__instance.IsSelected(obj))
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
    foreach (var obj in __instance.SelectedObjects)
    {
      if (obj is SelectedRoof sr)
      {
        sr.Dispose();
      }
    }
  }
}
