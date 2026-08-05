using HarmonyLib;
using UnityEngine;
using Verse;
using RimWorld;

using SolarWeb.Stratum.Hooks;

namespace SolarWeb.Stratum.Patches;

[HarmonyPatch(typeof(GenThing))]
public static class GenThing_Patch
{
  [HarmonyPatch(nameof(GenThing.TrueCenter), [typeof(Thing)])]
  [HarmonyPostfix]
  public static void TrueCenter_Postfix(Thing t, ref Vector3 __result)
  {
    if (!Utilities.RoofBuildings.IsRoofBuildingOrBlueprintOrFrame(t)) return;

    var map = t.Map;
    if (map != null)
    {
      var hookResult = MapHookRegistry.GetRoofBuildingTrueCenter(t, __result, map);
      if (hookResult.HasValue)
      {
        __result = hookResult.Value;
        return;
      }
    }
    
    var fallback = Utilities.RoofBuildings.GetRoofBuildingTrueCenter(t, __result);
    if (fallback.HasValue)
    {
      __result = fallback.Value;
    }
  }
}
