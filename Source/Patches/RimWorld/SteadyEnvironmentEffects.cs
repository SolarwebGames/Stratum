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
  internal static readonly AccessTools.FieldRef<SteadyEnvironmentEffects, ModuleBase> SnowNoiseRef =
    AccessTools.FieldRefAccess<SteadyEnvironmentEffects, ModuleBase>("snowNoise");

  private static readonly ConditionalWeakTable<Map, SkylightCoating> CoatingCache = [];

  private static Map? cachedMap;
  private static SkylightCoating? cachedCoating;
  private static int cachedTick = -1;
  private static float cachedSnowRate;
  private static float cachedOutdoorMeltAmount;

  [HarmonyPatch("DoCellSteadyEffects")]
  [HarmonyPostfix]
  public static void DoCellSteadyEffects_Postfix(SteadyEnvironmentEffects __instance, IntVec3 c, Map ___map)
  {
    if (!Stratum.Settings.enableSkylightCoating) return;

    var roof = ___map.roofGrid.RoofAt(c);
    if (roof == null || !RoofStatCache.IsCustomRoof(roof)) return;

    int currentTick = Find.TickManager.TicksGame;
    if (cachedMap != ___map || cachedTick != currentTick)
    {
      cachedMap = ___map;
      cachedTick = currentTick;
      cachedSnowRate = ___map.weatherManager.SnowRate;
      float outdoorTemp = ___map.mapTemperature.OutdoorTemp;
      cachedOutdoorMeltAmount = MeltAmountAt(outdoorTemp);

      if (!CoatingCache.TryGetValue(___map, out cachedCoating))
      {
        cachedCoating = ___map.GetComponent<SkylightCoating>();
        if (cachedCoating != null)
        {
          CoatingCache.Add(___map, cachedCoating);
        }
      }
    }

    if (cachedSnowRate <= 0.001f && cachedOutdoorMeltAmount <= 0f) return;
    if (cachedCoating == null) return;

    int idx = ___map.cellIndices.CellToIndex(c);
    float curSnow = cachedCoating.GetSnowLevel(idx);

    float newSnow = curSnow;
    if (cachedSnowRate > 0.001f && curSnow < 1f)
    {
      float value = cachedCoating.GetOrComputeSnowNoise(idx, c, __instance);
      newSnow += 0.046f * cachedSnowRate * value;
    }

    if (cachedOutdoorMeltAmount > 0f && newSnow > 0f)
    {
      newSnow -= cachedOutdoorMeltAmount;
    }

    newSnow = Mathf.Clamp01(newSnow);
    if (newSnow != curSnow)
    {
      cachedCoating.SetSnowLevel(idx, c, newSnow);
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
