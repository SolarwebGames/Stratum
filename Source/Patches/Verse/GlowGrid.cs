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
    float transparency;

    if (roof != null)
    {
      if (!RoofStatCache.IsSkylight(roof)) return true;
      transparency = RoofStatCache.GetEffectiveTransparency(roof, CoatingFor(___map), c);
      if (transparency <= 0f) return true;
    }
    else
    {
      // An open cell. Vanilla always gives it full sunlight, which is right until a vertical-levels
      // mod puts a floor over it -- at which point the cell renders dark but would still grow crops
      // at full rate. Only take the call over when a subscriber actually reports occlusion.
      if (!Stratum.Settings.enableUpperLevelSkyOcclusion) return true;
      if (!MapHookRegistry.HasSkyGlowMultiplierHandlers(___map)) return true;
      transparency = 1f;
    }

    float skyMultiplier = MapHookRegistry.GetCellSkyGlowMultiplier(___map, c, 1f);

    // Nothing overhead: hand an open cell straight back to vanilla rather than reimplementing it.
    if (roof == null && skyMultiplier >= 0.999f) return true;

    float skyGlow = ___map.skyManager.CurSkyGlow * transparency * skyMultiplier;
    if (skyGlow >= 1f)
    {
      __result = 1f;
      return false;
    }

    Color32 accumulated = AccumulatedGlowAt(__instance, c, ignoreCavePlants);
    if (accumulated.a == 1)
    {
      __result = 1f;
      return false;
    }

    float maxAccumulated = Mathf.Max(accumulated.r, Mathf.Max(accumulated.g, accumulated.b)) / 255f * 3.6f;
    maxAccumulated = Mathf.Min(0.5f, maxAccumulated);
    __result = Mathf.Max(skyGlow, maxAccumulated);
    return false;
  }

  /// <summary>
  /// Vanilla's private accumulator, which honours <c>ignoreCavePlants</c>. The public
  /// <c>VisualGlowAt</c> hardcodes it to false, so calling that instead silently diverged from the
  /// method we are replacing -- harmless while this only ran for skylight cells, but not once open
  /// cells route through here too.
  /// </summary>
  private static readonly System.Func<GlowGrid, IntVec3, bool, Color32>? accumulatedGlowAt =
    AccessTools.MethodDelegate<System.Func<GlowGrid, IntVec3, bool, Color32>>(
      AccessTools.Method(typeof(GlowGrid), "GetAccumulatedGlowAt", [typeof(IntVec3), typeof(bool)]));

  private static Color32 AccumulatedGlowAt(GlowGrid instance, IntVec3 c, bool ignoreCavePlants)
  {
    return accumulatedGlowAt != null
      ? accumulatedGlowAt(instance, c, ignoreCavePlants)
      : instance.VisualGlowAt(c);
  }

  // GroundGlowAt is one of the hottest methods in the game; the Map overload of
  // GetEffectiveTransparency does a GetComponent list scan, so resolve the coating once per map.
  private static readonly System.Runtime.CompilerServices.ConditionalWeakTable<Map, SkylightCoating> coatingCache = new();

  private static SkylightCoating? CoatingFor(Map map)
  {
    if (coatingCache.TryGetValue(map, out var coating)) return coating;

    var resolved = map.GetComponent<SkylightCoating>();
    // A null component cannot be memoised in a ConditionalWeakTable, but every map gets one, so
    // the uncached path is effectively unreachable rather than hot.
    if (resolved != null) coatingCache.Add(map, resolved);
    return resolved;
  }
}

