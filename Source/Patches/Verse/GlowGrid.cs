using HarmonyLib;
using SolarWeb.Stratum.Stats;
using UnityEngine;
using Verse;

namespace SolarWeb.Stratum.Patches;

[HarmonyPatch]
public static class GlowGrid_Patch
{
  [HarmonyPatch(typeof(GlowGrid), nameof(GlowGrid.GroundGlowAt))]
  [HarmonyPrefix]
  public static bool GroundGlowAt_Prefix(GlowGrid __instance, IntVec3 c, ref float __result, bool ignoreSky, Map ___map)
  {
    if (ignoreSky) return true;
    if (___map == null || ___map.skyManager == null || ___map.roofGrid == null) return true;

    var roof = ___map.roofGrid.RoofAt(c);
    if (roof != null && RoofStatCache.IsSkylight(roof))
    {
      float transparency = RoofStatCache.GetTransparency(roof);
      if (transparency > 0f)
      {
        float skyGlow = ___map.skyManager.CurSkyGlow * transparency;
        if (skyGlow >= 1f)
        {
          __result = 1f;
          return false;
        }

        Color32 accumulated = __instance.VisualGlowAt(c);
        if (accumulated.a == 1)
        {
          __result = 1f;
          return false;
        }

        // Original game logic for roofs uses max component of accumulated glow scaled by 3.6, capped at 0.5.
        float maxAccumulated = (float)Mathf.Max(accumulated.r, Mathf.Max(accumulated.g, accumulated.b)) / 255f * 3.6f;
        maxAccumulated = Mathf.Min(0.5f, maxAccumulated);
        __result = Mathf.Max(skyGlow, maxAccumulated);
        return false;
      }
    }

    return true;
  }
}

