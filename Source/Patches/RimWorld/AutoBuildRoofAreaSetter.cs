using HarmonyLib;
using RimWorld;
using Verse;

namespace SolarWeb.Stratum.Patches;

[HarmonyPatch]
public static class AutoBuildRoofAreaSetter_Patch
{
  [HarmonyPatch(typeof(AutoBuildRoofAreaSetter), nameof(AutoBuildRoofAreaSetter.TryGenerateAreaFor))]
  [HarmonyPrefix]
  public static bool TryGenerateAreaFor_Prefix()
  {
    return false; // Disable vanilla auto-roof building
  }
}
