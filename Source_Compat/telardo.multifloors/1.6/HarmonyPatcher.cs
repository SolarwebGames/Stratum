using HarmonyLib;
using Verse;

namespace SolarWeb.Stratum.MultiFloors;

[StaticConstructorOnStartup]
internal static class HarmonyPatcher
{
  static HarmonyPatcher()
  {
    var harmony = new Harmony("com.SolarWeb.Stratum.MultiFloors");
    harmony.PatchAll();

    MultiFloorSubscribers.Register();
  }
}
