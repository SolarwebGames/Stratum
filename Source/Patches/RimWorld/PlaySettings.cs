using HarmonyLib;
using Verse;
using RimWorld;
using SolarWeb.Stratum.Hooks;
using SolarWeb.Stratum.Utilities;

namespace SolarWeb.Stratum.Patches;

[HarmonyPatch(typeof(PlaySettings))]
public static class PlaySettings_Patch
{
  [HarmonyPatch(nameof(PlaySettings.ExposeData))]
  [HarmonyPostfix]
  public static void ExposeData_Postfix()
  {
    Scribe_Values.Look(ref RoofBuildings.showRoofBuildings, "showRoofBuildings", false);
  }

  [HarmonyPatch("DoMapControls")]
  [HarmonyPostfix]
  public static void DoMapControls_Postfix(WidgetRow row)
  {
    MapHookRegistry.Notify_PlaySettingsDoMapControls(row);
  }
}
