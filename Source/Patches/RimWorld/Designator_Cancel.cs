using HarmonyLib;
using Verse;
using RimWorld;
using SolarWeb.Stratum.Stats;

namespace SolarWeb.Stratum.Patches;

[HarmonyPatch]
public static class Designator_Cancel_Patch
{
  [HarmonyPatch(typeof(Designator_Cancel), nameof(Designator_Cancel.CanDesignateCell))]
  [HarmonyPostfix]
  public static void CanDesignateCell_Postfix(IntVec3 c, Designator_Cancel __instance, ref AcceptanceReport __result)
  {
    if (__result.Accepted) return;

    if (__instance.Map.areaManager.NoRoof[c] && RoofStatCache.IsCustomRoof(__instance.Map.roofGrid.RoofAt(c)))
    {
      __result = true;
    }
  }

  [HarmonyPatch(typeof(Designator_Cancel), nameof(Designator_Cancel.DesignateSingleCell))]
  [HarmonyPrefix]
  public static void DesignateSingleCell_Prefix(IntVec3 c, Designator_Cancel __instance)
  {
    if (__instance.Map.areaManager.NoRoof[c] && RoofStatCache.IsCustomRoof(__instance.Map.roofGrid.RoofAt(c)))
    {
      __instance.Map.areaManager.NoRoof[c] = false;
      __instance.Map.areaManager.NoRoof.MarkForDraw();
    }
  }

  [HarmonyPatch(typeof(Designator_Cancel), nameof(Designator_Cancel.CanDesignateThing))]
  [HarmonyPostfix]
  public static void CanDesignateThing_Postfix(Thing t, ref AcceptanceReport __result)
  {
    if (__result.Accepted) return;

    if (t is ICancelableByDesignator cancelable && cancelable.CanCancel)
    {
      __result = true;
    }
  }

  [HarmonyPatch(typeof(Designator_Cancel), nameof(Designator_Cancel.DesignateThing))]
  [HarmonyPrefix]
  public static bool DesignateThing_Prefix(Thing t)
  {
    if (t is ICancelableByDesignator cancelable && cancelable.CanCancel)
    {
      cancelable.CancelByDesignator();
      return false;
    }
    return true;
  }
}
