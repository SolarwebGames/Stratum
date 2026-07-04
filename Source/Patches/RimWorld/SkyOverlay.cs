using HarmonyLib;
using UnityEngine;
using Verse;

namespace SolarWeb.Stratum.Patches;

[HarmonyPatch(typeof(SkyOverlay))]
public static class SkyOverlay_Patch
{
  [HarmonyPatch(nameof(SkyOverlay.DrawWorldOverlay), [typeof(Map), typeof(Material), typeof(float), typeof(int)])]
  [HarmonyPrefix]
  public static void DrawWorldOverlay_Prefix(Material mat)
  {
    if (mat != null && mat.renderQueue < 4000)
    {
      mat.renderQueue = 4600;
    }
  }
}
