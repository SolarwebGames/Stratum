using HarmonyLib;
using RimWorld;
using SolarWeb.Stratum.DefModExtensions;
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
  [HarmonyPostfix]
  public static void SetRoof_Postfix(IntVec3 c, RoofDef def, Map ___map)
  {
    var vfx = ___map.GetComponent<RoofVFXMapComponent>();
    vfx?.Notify_RoofChanged(c);

    var solar = ___map.GetComponent<SolarRoofMapComponent>();
    solar?.Notify_RoofChanged(c);

    // Force invalidation of Room and District roof caches
    var room = c.GetRoom(___map);
    if (room != null)
    {
      foreach (var district in room.Districts)
      {
        district.Notify_RoofChanged();
      }
    }

    if (def != null && RoofStatCache.IsCustomRoof(def))
    {
      var integrity = ___map.GetComponent<RoofIntegrityGrid>();

      ThingDef? stuff = null;
      if (DebugSettings.godMode)
      {
        var designator = Find.DesignatorManager.SelectedDesignator as AI.Designators.BuildCustomRoof;
        if (designator != null) stuff = designator.StuffDef;
      }

      integrity?.InitializeRoof(c, def, stuff);
    }
  }
}
