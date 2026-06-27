using HarmonyLib;
using Verse;

using SolarWeb.Stratum.Stats;
using SolarWeb.Stratum.Hooks;

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
  [HarmonyPrefix]
  public static bool SetRoof_Prefix(IntVec3 c, ref RoofDef def, Map ___map, out RoofDef? __state)
  {
    if (___map == null || ___map.roofGrid == null)
    {
      __state = null;
      return true;
    }

    var oldRoof = ___map.roofGrid.RoofAt(c);
    __state = oldRoof;

    var registry = MapHookRegistry.Get(___map);
    if (registry != null)
    {
      bool allow = true;
      RoofDef? newRoof = def;
      var handlers = registry.GetHandlers<MapHookRegistry.BeforeSetRoofHandler>(MapHookRegistry.HookId.BeforeSetRoof);
      if (handlers != null)
      {
        for (int i = 0; i < handlers.Count; i++)
        {
          try
          {
            handlers[i](___map, c, oldRoof, ref newRoof, ref allow);
          }
          catch (System.Exception ex)
          {
            StratumLog.Error($"Error in BeforeSetRoof subscriber: {ex}");
          }
        }
      }
      def = newRoof!;
      if (!allow)
      {
        return false;
      }
    }
    return true;
  }

  [HarmonyPatch(typeof(RoofGrid), nameof(RoofGrid.SetRoof))]
  [HarmonyPostfix]
  public static void SetRoof_Postfix(IntVec3 c, Map ___map, RoofDef? __state)
  {
    if (___map == null || ___map.roofGrid == null) return;
    var currentRoof = ___map.roofGrid.RoofAt(c);
    if (currentRoof == __state) return;

    var registry = MapHookRegistry.Get(___map);
    if (registry != null)
    {
      var handlers = registry.GetHandlers<MapHookRegistry.RoofChangedHandler>(MapHookRegistry.HookId.RoofChanged);
      if (handlers != null)
      {
        for (int i = 0; i < handlers.Count; i++)
        {
          try
          {
            handlers[i](___map, c, __state, currentRoof);
          }
          catch (System.Exception ex)
          {
            StratumLog.Error($"Error in RoofChanged subscriber: {ex}");
          }
        }
      }
    }
  }
}
