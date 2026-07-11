using HarmonyLib;
using UnityEngine;
using Verse;

using SolarWeb.Stratum.Stats;
using SolarWeb.Stratum.Hooks;
using SolarWeb.Stratum.MapComponents;

namespace SolarWeb.Stratum.Patches;

[HarmonyPatch(typeof(GlowGrid))]
public static class GlowGrid_Patch
{
  [HarmonyPatch(nameof(GlowGrid.GroundGlowAt))]
  [HarmonyPrefix]
  [HarmonyBefore("realtiltmod")]
  public static bool GroundGlowAt_Prefix(GlowGrid __instance, IntVec3 c, ref float __result, bool ignoreCavePlants, bool ignoreSky, Map ___map)
  {
    if (___map == null) return true;

    var globalHandlers = MapHookRegistry.GetGlobalHandlers<MapHookRegistry.GroundGlowHandler>(MapHookRegistry.HookId.GroundGlow);
    if (globalHandlers != null)
    {
      float result = 0f;
      for (int i = 0; i < globalHandlers.Count; i++)
      {
        try
        {
          if (globalHandlers[i](__instance, ___map, c, ignoreCavePlants, ignoreSky, ref result))
          {
            __result = result;
            return false;
          }
        }
        catch (System.Exception ex)
        {
          StratumLog.Error($"Error in global GroundGlow handler: {ex}");
        }
      }
    }

    if (ignoreSky) return true;
    if (___map.skyManager == null || ___map.roofGrid == null) return true;

    var roof = ___map.roofGrid.RoofAt(c);
    if (roof != null && RoofStatCache.IsSkylight(roof))
    {
      float transparency = RoofStatCache.GetTransparency(roof);
      var coating = ___map.GetComponent<SkylightCoating>();
      if (coating != null)
      {
        transparency *= (1f - coating.GetCoatingOpacity(c));
      }
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

