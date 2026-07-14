using HarmonyLib;
using RimWorld;
using System.Runtime.CompilerServices;
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

  [HarmonyPatch("DoCellSteadyEffects")]
  [HarmonyPostfix]
  public static void DoCellSteadyEffects_Postfix(SteadyEnvironmentEffects __instance, IntVec3 c, Map ___map)
  {
    if (!Stratum.Settings.enableSkylightCoating) return;
    var roof = ___map.roofGrid.RoofAt(c);
    if (roof == null || !RoofStatCache.IsCustomRoof(roof)) return;

    if (!CoatingCache.TryGetValue(___map, out var coating))
    {
      coating = ___map.GetComponent<SkylightCoating>();
      CoatingCache.Add(___map, coating);
    }
    if (coating == null) return;

    float snowRate = ___map.weatherManager.SnowRate;
    if (snowRate > 0.001f)
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
      float curSnow = coating.GetSnowLevel(c);
      if (curSnow < 1f)
      {
        coating.SetSnowLevel(c, curSnow + depthToAdd);
      }
    }

    float outdoorTemp = ___map.mapTemperature.OutdoorTemp;
    float outdoorMeltAmount = MeltAmountAt(outdoorTemp);
    if (outdoorMeltAmount > 0f)
    {
      float curSnow = coating.GetSnowLevel(c);
      if (curSnow > 0f)
      {
        coating.SetSnowLevel(c, curSnow - outdoorMeltAmount);
      }
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
