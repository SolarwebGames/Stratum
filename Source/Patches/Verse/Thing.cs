using System;
using HarmonyLib;
using UnityEngine;
using Verse;
using SolarWeb.Stratum.DefModExtensions;
using SolarWeb.Stratum.Hooks;
using SolarWeb.Stratum.Utilities;

namespace SolarWeb.Stratum.Patches;

[HarmonyPatch(typeof(Thing))]
public static class Thing_Patch
{
  [HarmonyPatch(nameof(Thing.DrawPos), MethodType.Getter)]
  [HarmonyPostfix]
  public static void DrawPos_Postfix(Thing __instance, ref Vector3 __result)
  {
    if (!RoofBuildings.IsRoofBuildingOrBlueprintOrFrame(__instance)) return;
    var map = __instance.Map;
    if (map != null)
    {
      var registry = MapHookRegistry.Get(map);
      if (registry != null)
      {
        var handlers = registry.GetHandlers<MapHookRegistry.RoofBuildingDrawPosHandler>(MapHookRegistry.HookId.RoofBuildingDrawPos);
        if (handlers != null)
        {
          for (int i = 0; i < handlers.Count; i++)
          {
            try
            {
              var res = handlers[i](__instance, __result);
              if (res.HasValue)
              {
                __result = res.Value;
                return;
              }
            }
            catch (Exception ex)
            {
              StratumLog.Error($"Error in RoofBuildingDrawPos subscriber: {ex}");
            }
          }
        }
      }
    }

    var fallback = RoofBuildings.GetRoofBuildingDrawPos(__instance, __result);
    if (fallback.HasValue)
    {
      __result = fallback.Value;
    }
  }

  [HarmonyPatch(nameof(Thing.DynamicDrawPhaseAt))]
  [HarmonyPrefix]
  public static bool DynamicDrawPhaseAt_Prefix(Thing __instance)
  {
    if (__instance == null || __instance.def == null) return true;
    if (RoofBuildings.IsRoofBuildingOrBlueprintOrFrame(__instance))
    {
      if (!RoofBuildings.ShouldRenderRoofBuilding(__instance))
      {
        return false;
      }
    }
    return true;
  }

  [HarmonyPatch(nameof(Thing.Print))]
  [HarmonyPrefix]
  public static bool Print_Prefix(Thing __instance)
  {
    if (__instance == null || __instance.def == null) return true;
    if (RoofBuildings.IsRoofBuildingOrBlueprintOrFrame(__instance))
    {
      if (!RoofBuildings.ShouldRenderRoofBuilding(__instance))
      {
        return false;
      }
    }
    return true;
  }

  [HarmonyPatch(nameof(Thing.DrawGUIOverlay))]
  [HarmonyPrefix]
  public static bool DrawGUIOverlay_Prefix(Thing __instance)
  {
    if (__instance == null || __instance.def == null) return true;
    if (RoofBuildings.IsRoofBuildingOrBlueprintOrFrame(__instance))
    {
      if (!RoofBuildings.ShouldRenderRoofBuilding(__instance))
      {
        return false;
      }
    }
    return true;
  }
}
