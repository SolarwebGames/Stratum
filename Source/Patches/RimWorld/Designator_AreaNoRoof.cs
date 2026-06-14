using HarmonyLib;
using RimWorld;
using Verse;

namespace SolarWeb.Stratum.Patches;

[HarmonyPatch]
public static class Designator_AreaNoRoof_Patch
{
  [HarmonyPatch(typeof(Designator_AreaNoRoof), nameof(Designator_AreaNoRoof.CanDesignateCell))]
  [HarmonyPostfix]
  public static void Postfix(IntVec3 c, Designator_AreaNoRoof __instance, ref AcceptanceReport __result)
  {
    __result = true;
    if (!c.Roofed(__instance.Map) || !c.InBounds(__instance.Map) || c.Fogged(__instance.Map) || __instance.Map.areaManager.NoRoof[c])
    {
      __result = false;
    }

    var roofDef = __instance.Map.roofGrid.RoofAt(c);
    if (roofDef != null && roofDef.isThickRoof)
    {
      __result = "MessageNothingCanRemoveThickRoofs".Translate();
    }
  }
}
