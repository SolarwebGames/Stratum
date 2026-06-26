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
      registry.Notify_BeforeSetRoof(c, oldRoof, ref newRoof, ref allow);
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
      registry.Notify_RoofChanged(c, __state, currentRoof);
    }
  }
}
