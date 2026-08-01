using HarmonyLib;
using RimWorld;
using System.Runtime.CompilerServices;
using UnityEngine;
using Verse;
using Verse.Noise;

using SolarWeb.Stratum.MapComponents;
using SolarWeb.Stratum.Stats;

namespace SolarWeb.Stratum.Patches;

[HarmonyPatch(typeof(SteadyEnvironmentEffects))]
public static class SteadyEnvironmentEffects_Patch
{
  internal static readonly AccessTools.FieldRef<SteadyEnvironmentEffects, ModuleBase> SnowNoiseRef =
    AccessTools.FieldRefAccess<SteadyEnvironmentEffects, ModuleBase>("snowNoise");

  // SteadyEnvironmentEffectsTick computes these once per tick before it starts calling
  // DoCellSteadyEffects, so reading them back is both cheaper and guaranteed in step with the
  // values vanilla used for this tick.
  private static readonly AccessTools.FieldRef<SteadyEnvironmentEffects, float> SnowRateRef =
    AccessTools.FieldRefAccess<SteadyEnvironmentEffects, float>("snowRate");
  private static readonly AccessTools.FieldRef<SteadyEnvironmentEffects, float> OutdoorMeltAmountRef =
    AccessTools.FieldRefAccess<SteadyEnvironmentEffects, float>("outdoorMeltAmount");

  private static readonly ConditionalWeakTable<Map, SkylightCoating> CoatingCache = [];

  [HarmonyPatch("DoCellSteadyEffects")]
  [HarmonyPostfix]
  public static void DoCellSteadyEffects_Postfix(SteadyEnvironmentEffects __instance, IntVec3 c, Map ___map)
  {
    if (!Stratum.Settings.enableSkylightCoating) return;

    float snowRate = SnowRateRef(__instance);
    float meltAmount = OutdoorMeltAmountRef(__instance);
    if (snowRate <= 0.001f && meltAmount <= 0f) return;

    var coating = GetCoating(___map);
    if (coating == null) return;

    var roof = ___map.roofGrid.RoofAt(c);
    if (roof == null || !RoofStatCache.IsCustomRoof(roof)) return;

    int idx = ___map.cellIndices.CellToIndex(c);
    float curSnow = coating.GetSnowLevel(idx);

    float newSnow = curSnow;
    if (snowRate > 0.001f && curSnow < 1f)
    {
      newSnow += 0.046f * snowRate * coating.GetOrComputeSnowNoise(idx, c, __instance);
    }

    if (meltAmount > 0f && newSnow > 0f)
    {
      newSnow -= meltAmount;
    }

    newSnow = Mathf.Clamp01(newSnow);
    if (newSnow != curSnow)
    {
      coating.SetSnowLevel(idx, c, newSnow);
    }
  }

  private static SkylightCoating? GetCoating(Map map)
  {
    if (CoatingCache.TryGetValue(map, out var coating)) return coating;

    coating = map.GetComponent<SkylightCoating>();
    if (coating != null) CoatingCache.Add(map, coating);
    return coating;
  }
}
