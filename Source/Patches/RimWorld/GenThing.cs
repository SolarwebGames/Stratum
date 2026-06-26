using HarmonyLib;
using UnityEngine;
using Verse;
using RimWorld;

using SolarWeb.Stratum.DefModExtensions;
using SolarWeb.Stratum.Hooks;
using SolarWeb.Stratum.Utilities;

namespace SolarWeb.Stratum.Patches;

[HarmonyPatch(typeof(GenThing))]
public static class GenThing_Patch
{
  [HarmonyPatch(nameof(GenThing.TrueCenter), [typeof(Thing)])]
  [HarmonyPostfix]
  public static void TrueCenter_Postfix(Thing t, ref Vector3 __result)
  {
    if (t == null || t.def == null) return;

    var map = t.Map;
    if (map != null)
    {
      var registry = MapHookRegistry.Get(map);
      if (registry != null)
      {
        var res = registry.GetRoofBuildingTrueCenter(t, __result);
        if (res.HasValue)
        {
          __result = res.Value;
        }
      }
    }
  }
}
