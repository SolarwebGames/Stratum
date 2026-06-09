using HarmonyLib;
using RimWorld;
using Verse;

namespace SolarWeb.Stratum.Patches;

[HarmonyPatch]
public static class GenConstruct_BlocksConstruction_Patch
{
  [HarmonyPatch(typeof(GenConstruct), nameof(GenConstruct.BlocksConstruction))]
  [HarmonyPrefix]
  public static bool BlocksConstruction_Prefix(Thing constructible, Thing t, ref bool __result)
  {
    if (constructible == null || t == null)
    {
      __result = false;
      return false;
    }

    if (t == constructible)
    {
      __result = false;
      return false;
    }

    ThingDef? thingDef = null;
    if (constructible is Blueprint)
    {
      thingDef = constructible.def;
    }
    else if (constructible is Frame)
    {
      thingDef = constructible.def.entityDefToBuild?.blueprintDef;
    }
    else
    {
      thingDef = constructible.def.blueprintDef;
    }

    if (thingDef == null || thingDef.entityDefToBuild == null)
    {
      __result = false;
      return false;
    }

    return true;
  }
}
