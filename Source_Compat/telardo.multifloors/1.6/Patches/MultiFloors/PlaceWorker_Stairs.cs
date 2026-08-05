using HarmonyLib;
using Verse;
using global::MultiFloors;

using SolarWeb.Stratum.Stats;

namespace SolarWeb.Stratum.MultiFloors.Patches.MultiFloors;

[HarmonyPatch(typeof(PlaceWorker_Stairs))]
public static class PlaceWorker_Stairs_Patch
{
  // MultiFloors rejects upstairs placement for several unrelated reasons (basement level, room
  // too small, room psychologically outdoors). Only the "no roof overhead" rejection is one
  // Stratum should override, so match on that specific reason rather than accepting any failure.
  private static string? cachedNoRoofReason;

  private static string NoRoofReason =>
    cachedNoRoofReason ??= "MF_CantBuildUpStairWithoutRoof".Translate().ToString();

  [HarmonyPatch(nameof(PlaceWorker_Stairs.AllowsPlacing))]
  [HarmonyPostfix]
  public static void AllowsPlacing_Postfix(BuildableDef checkingDef, IntVec3 loc, Rot4 rot, Map map, ref AcceptanceReport __result)
  {
    if (__result.Accepted) return;
    if (checkingDef == null || map == null || !loc.InBounds(map)) return;
    if (!checkingDef.IsUpstairs()) return;
    if (__result.Reason != NoRoofReason) return;

    // MultiFloors counts the room's open-roof cells and does not recognise Stratum's custom
    // roof defs as cover, so a properly roofed room reads as open. Accept when the cell really
    // is covered by a thick or Stratum-built roof.
    var roof = map.roofGrid?.RoofAt(loc);
    if (roof != null && (roof.isThickRoof || RoofStatCache.IsCustomRoof(roof)))
    {
      __result = true;
    }
  }
}
