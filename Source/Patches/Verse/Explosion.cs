using HarmonyLib;
using Verse;
using SolarWeb.Stratum.Things;

namespace SolarWeb.Stratum.Patches;

[HarmonyPatch(typeof(Explosion))]
public static class Explosion_Patch
{
  [HarmonyPatch("AffectCell")]
  [HarmonyPrefix]
  public static bool AffectCell_Prefix(Explosion __instance, IntVec3 c)
  {
    if (__instance is RoofExplosion roofExplosion)
    {
      roofExplosion.AffectRoofCell(c);
      return false;
    }
    return true;
  }
}
