using HarmonyLib;
using Verse;

namespace SolarWeb.Stratum.Patches;

[StaticConstructorOnStartup]
internal static class HarmonyPatcher
{
  static HarmonyPatcher()
  {
    var harmony = new Harmony("com.solarweb.Stratum");
    harmony.PatchAll();
  }
}
