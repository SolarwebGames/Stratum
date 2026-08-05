using HarmonyLib;
using Verse;
using RimWorld;

using SolarWeb.Stratum.Hooks;
using SolarWeb.Stratum.Utilities;

namespace SolarWeb.Stratum.Patches;

[HarmonyPatch(typeof(ThingSelectionUtility))]
public static class ThingSelectionUtility_Patch
{
  [HarmonyPatch(nameof(ThingSelectionUtility.SelectableByMapClick))]
  [HarmonyPrefix]
  public static bool SelectableByMapClick_Prefix(Thing t, ref bool __result)
  {
    if (t == null) return true;

    var map = t.Map;
    if (map != null)
    {
      var hookResult = MapHookRegistry.CheckRoofBuildingSelectable(t, map);
      if (hookResult.HasValue)
      {
        __result = hookResult.Value;
        return false;
      }
    }

    var fallback = RoofBuildings.CheckRoofBuildingSelectable(t);
    if (fallback.HasValue)
    {
      __result = fallback.Value;
      return false;
    }

    return true;
  }
}
