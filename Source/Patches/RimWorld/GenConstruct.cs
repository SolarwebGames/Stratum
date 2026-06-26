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
      var registry = MapHookRegistry.Get(map);
      if (registry != null)
      {
        var val = registry.CheckBlocksConstruction(constructible, t);
        if (val.HasValue)
        {
          __result = val.Value;
          return false;
        }
      }
    }
    return true;
  }
}
