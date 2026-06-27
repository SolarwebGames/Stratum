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
    var handlers = MapHookRegistry.GetGlobalHandlers<MapHookRegistry.PlaySettingsDoMapControlsHandler>(MapHookRegistry.HookId.PlaySettingsDoMapControls);
    if (handlers != null)
    {
      for (int i = 0; i < handlers.Count; i++)
      {
        try
        {
          handlers[i](row);
        }
        catch (System.Exception ex)
        {
          StratumLog.Error($"Error in PlaySettingsDoMapControls subscriber: {ex}");
        }
      }
    }

    try
    {
      RoofBuildings.DoMapControls(row);
    }
    catch (System.Exception ex)
    {
      StratumLog.Error($"Error in built-in DoMapControls: {ex}");
    }
  }
}
