using HarmonyLib;
using Verse;

namespace SolarWeb.Stratum.DubsSkylights;

[StaticConstructorOnStartup]
internal static class HarmonyPatcher
{
  static HarmonyPatcher()
  {
    var harmony = new Harmony("com.SolarWeb.Stratum.DubsSkylights");
    harmony.PatchAll();

    // Unpatch Dubs Skylights' GroundGlowAt and SectionLayer_LightingOverlay patches to avoid conflicts with Stratum's lighting systems
    var originalGlow = AccessTools.Method(typeof(GlowGrid), nameof(GlowGrid.GroundGlowAt));
    if (originalGlow != null)
    {
      harmony.Unpatch(originalGlow, HarmonyPatchType.Postfix, "Dubwise.Dubs_Skylights");
    }

    var originalRegen = AccessTools.Method(typeof(SectionLayer_LightingOverlay), "Regenerate");
    if (originalRegen != null)
    {
      harmony.Unpatch(originalRegen, HarmonyPatchType.All, "Dubwise.Dubs_Skylights");
    }

    StratumHookSubscribers.Register();
  }
}
