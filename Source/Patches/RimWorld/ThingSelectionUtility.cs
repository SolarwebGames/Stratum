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
      var registry = MapHookRegistry.Get(map);
      if (registry != null)
      {
        var res = registry.CheckRoofBuildingSelectable(t);
        if (res.HasValue)
        {
          __result = res.Value;
          return false;
        }
      }
    }
    return true;
  }
}
