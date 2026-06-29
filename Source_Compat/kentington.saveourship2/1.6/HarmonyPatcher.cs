using HarmonyLib;
using Verse;

namespace SolarWeb.Stratum.SOS2;

[StaticConstructorOnStartup]
internal static class HarmonyPatcher
{
  static HarmonyPatcher()
  {
    var harmony = new Harmony("com.SolarWeb.Stratum.SOS2");
    harmony.PatchAll();
  }
}
