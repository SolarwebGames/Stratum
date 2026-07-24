using System;
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
  private static readonly AccessTools.FieldRef<SteadyEnvironmentEffects, ModuleBase> SnowNoiseRef =
    AccessTools.FieldRefAccess<SteadyEnvironmentEffects, ModuleBase>("snowNoise");

  private static readonly ConditionalWeakTable<Map, SkylightCoating> CoatingCache = [];

  private static System.WeakReference<Map>? lastMapRef;
  private static System.WeakReference<SkylightCoating>? lastCoatingRef;

  [HarmonyPatch("DoCellSteadyEffects")]
  [HarmonyPostfix]
  public static void DoCellSteadyEffects_Postfix(SteadyEnvironmentEffects __instance, IntVec3 c, Map ___map)
  {
    if (!Stratum.Settings.enableSkylightCoating) return;

    float snowRate = ___map.weatherManager.SnowRate;
    float outdoorTemp = ___map.mapTemperature.OutdoorTemp;
    float outdoorMeltAmount = MeltAmountAt(outdoorTemp);

    if (snowRate <= 0.001f && outdoorMeltAmount <= 0f) return;

    var roof = ___map.roofGrid.RoofAt(c);
    if (roof == null || !RoofStatCache.IsCustomRoof(roof)) return;

    SkylightCoating? coating = null;
    if (lastMapRef != null && lastMapRef.TryGetTarget(out var cachedMap) && cachedMap == ___map)
    {
      lastCoatingRef?.TryGetTarget(out coating);
    }
    else
    {
      if (!CoatingCache.TryGetValue(___map, out coating))
      {
        coating = ___map.GetComponent<SkylightCoating>();
        if (coating != null)
        {
          CoatingCache.Add(___map, coating);
        }
      }
      lastMapRef = new System.WeakReference<Map>(___map);
      lastCoatingRef = coating != null ? new System.WeakReference<SkylightCoating>(coating) : null;
    }

    if (coating == null) return;

    float curSnow = coating.GetSnowLevel(c);

    if (snowRate > 0.001f && curSnow < 1f)
    {
      var snowNoise = SnowNoiseRef(__instance);
      if (snowNoise == null)
      {
        snowNoise = new Perlin(0.03999999910593033, 2.0, 0.5, 5, Rand.Range(0, 651431), QualityMode.Medium);
        SnowNoiseRef(__instance) = snowNoise;
      }

      float value = snowNoise.GetValue(c);
      value += 1f;
      value *= 0.5f;
      if (value < 0.5f)
      {
        value = 0.5f;
      }

      float depthToAdd = 0.046f * snowRate * value;
      float newSnow = Mathf.Clamp01(curSnow + depthToAdd);
      coating.SetSnowLevel(c, newSnow);
      curSnow = newSnow;
    }

    if (outdoorMeltAmount > 0f && curSnow > 0f)
    {
      coating.SetSnowLevel(c, curSnow - outdoorMeltAmount);
    }
  }

  private static float MeltAmountAt(float temperature)
  {
    if (temperature < 0f)
    {
      return 0f;
    }
    if (temperature < 10f)
    {
      return temperature * temperature * 0.0058f * 0.1f;
    }
    return temperature * 0.0058f;
  }
}
