using HarmonyLib;
using Verse;
using RimWorld;

using SolarWeb.Stratum.Hooks;

namespace SolarWeb.Stratum.Patches;

[HarmonyPatch(typeof(GenConstruct))]
public static class GenConstruct_Patch
{
  [HarmonyPatch(nameof(GenConstruct.BlocksConstruction))]
  [HarmonyPrefix]
  public static bool BlocksConstruction_Prefix(Thing constructible, Thing t, ref bool __result)
  {
    var map = constructible.Map ?? t.Map;
    if (map != null)
    {
      var hookResult = MapHookRegistry.CheckBlocksConstruction(constructible, t, map);
      if (hookResult.HasValue)
      {
        __result = hookResult.Value;
        return false;
      }
    }

    var fallback = Utilities.RoofBuildings.CheckBlocksConstruction(constructible, t);
    if (fallback.HasValue)
    {
      __result = fallback.Value;
      return false;
    }

    return true;
  }

  [HarmonyPatch(nameof(GenConstruct.CanPlaceBlueprintOver))]
  [HarmonyPostfix]
  public static void CanPlaceBlueprintOver_Postfix(BuildableDef newDef, ThingDef oldDef, ref bool __result)
  {
    var globalHandlers = MapHookRegistry.GetGlobalHandlers<MapHookRegistry.CanPlaceBlueprintOverHandler>(MapHookRegistry.HookId.CanPlaceBlueprintOver);
    if (globalHandlers != null)
    {
      for (int i = 0; i < globalHandlers.Count; i++)
      {
        try
        {
          var val = globalHandlers[i](newDef, oldDef);
          if (val.HasValue)
          {
            __result = val.Value;
            return;
          }
        }
        catch (System.Exception ex)
        {
          StratumLog.Error($"Error in GlobalCanPlaceBlueprintOver subscriber: {ex}");
        }
      }
    }

    var fallback = Utilities.RoofBuildings.CheckCanPlaceBlueprintOver(newDef, oldDef);
    if (fallback.HasValue)
    {
      __result = fallback.Value;
    }
  }
}
