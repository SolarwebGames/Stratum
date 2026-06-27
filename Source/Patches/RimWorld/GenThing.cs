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
      var registry = MapHookRegistry.Get(map);
      if (registry != null)
      {
        var handlers = registry.GetHandlers<MapHookRegistry.RoofBuildingTrueCenterHandler>(MapHookRegistry.HookId.RoofBuildingTrueCenter);
        if (handlers != null)
        {
          for (int i = 0; i < handlers.Count; i++)
          {
            try
            {
              var res = handlers[i](t, __result);
              if (res.HasValue)
              {
                __result = res.Value;
                return;
              }
            }
            catch (System.Exception ex)
            {
              StratumLog.Error($"Error in RoofBuildingTrueCenter subscriber: {ex}");
            }
          }
        }
      }
    }
    
    var fallback = Utilities.RoofBuildings.GetRoofBuildingTrueCenter(t, __result);
    if (fallback.HasValue)
    {
      __result = fallback.Value;
    }
  }
}
