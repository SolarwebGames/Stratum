using HarmonyLib;
using Verse;
using RimWorld;

using SolarWeb.Stratum.DefModExtensions;
using SolarWeb.Stratum.Hooks;

namespace SolarWeb.Stratum.Patches;

[HarmonyPatch(typeof(GenLeaving))]
public static class GenLeaving_Patch
{
  [HarmonyPatch(nameof(GenLeaving.DoLeavingsFor))]
  [HarmonyPrefix]
  public static bool DoLeavingsFor_Prefix(Thing diedThing, Map map, DestroyMode mode)
  {
    if (diedThing == null || diedThing.def == null || map == null) return true;
    if (diedThing.def.HasModExtension<RoofBuilding>() && mode == DestroyMode.KillFinalize)
    {
      var registry = MapHookRegistry.Get(map);
      if (registry != null)
      {
        registry.CheckRoofBuildingDestroyedDebris(diedThing);
      }
      return false;
    }
    return true;
  }
}
