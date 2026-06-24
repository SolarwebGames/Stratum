using HarmonyLib;
using SolarWeb.Stratum.MapComponents;
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
    if (roof != null && RoofStatCache.IsCustomRoof(roof))
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

  [HarmonyPatch(typeof(GlowGrid), nameof(GlowGrid.VisualGlowAt), [typeof(int)])]
  [HarmonyPostfix]
  public static void VisualGlowAt_Postfix(int index, Map ___map, ref Color32 __result)
  {
    if (___map == null || ___map.skyManager == null || ___map.roofGrid == null) return;
    var roof = ___map.roofGrid.RoofAt(index);
    if (roof == null) return;

    float transparency = RoofStatCache.GetTransparency(roof);
    if (transparency > 0f)
    {
      float skyGlow = ___map.skyManager.CurSkyGlow;
      if (skyGlow <= 0.01f)
      {
        // At night, ensure we still have the standard ambient light alpha for roofs
        if (__result.a < 100) __result.a = 100;
        return;
      }

      var integrity = ___map.GetComponent<RoofIntegrityGrid>();
      IntVec3 cell = ___map.cellIndices.IndexToCell(index);
      ThingDef? stuff = integrity?.GetStuff(cell);
      Color glassColor = RoofStatCache.GetGlassTint(roof, ___map, cell);

      // Add tinted sky light to the visual result
      float addedR = glassColor.r * skyGlow * transparency * 255f;
      float addedG = glassColor.g * skyGlow * transparency * 255f;
      float addedB = glassColor.b * skyGlow * transparency * 255f;

      int r = __result.r + (int)addedR;
      int g = __result.g + (int)addedG;
      int b = __result.b + (int)addedB;

      int max = Mathf.Max(r, Mathf.Max(g, b));

      // Ensure alpha is at least 100 (standard roof ambient) or the new brightness
      int finalA = Mathf.Max(100, max);

      if (finalA > 255)
      {
        __result.r = (byte)(r * 255 / finalA);
        __result.g = (byte)(g * 255 / finalA);
        __result.b = (byte)(b * 255 / finalA);
        __result.a = 255;
      }
      else
      {
        __result.r = (byte)r;
        __result.g = (byte)g;
        __result.b = (byte)b;
        __result.a = (byte)finalA;
      }
    }
  }

  [HarmonyPatch(typeof(GlowGrid), nameof(GlowGrid.VisualGlowAt), [typeof(IntVec3)])]
  [HarmonyPostfix]
  public static void VisualGlowAt_Postfix(IntVec3 c, Map ___map, ref Color32 __result)
  {
    var cellIndex = ___map.cellIndices.CellToIndex(c);
    VisualGlowAt_Postfix(cellIndex, ___map, ref __result);
  }
}

