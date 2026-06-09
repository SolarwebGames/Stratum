using HarmonyLib;
using RimWorld;
using SolarWeb.Stratum.Stats;
using SolarWeb.Stratum.MapComponents;
using Verse;

namespace SolarWeb.Stratum.Patches;

[HarmonyPatch]
public static class BeautyUtility_Patch
{
  [HarmonyPatch(typeof(BeautyUtility), nameof(BeautyUtility.CellBeauty))]
  [HarmonyPostfix]
  public static void Postfix(IntVec3 c, Map map, ref float __result)
  {
    var roof = map.roofGrid.RoofAt(c);
    if (roof != null && RoofStatCache.IsCustomRoof(roof))
    {
      var stuff = map.GetComponent<RoofIntegrityGrid>()?.GetStuff(c);
      __result += RoofStatCache.GetBeauty(roof, stuff);
    }
  }
}
