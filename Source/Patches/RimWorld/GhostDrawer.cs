using HarmonyLib;
using Verse;
using SolarWeb.Stratum.Utilities;
using SolarWeb.Stratum.DefModExtensions;

namespace SolarWeb.Stratum.Patches.RimWorld;

[HarmonyPatch(typeof(GhostDrawer))]
public static class GhostDrawer_Patch
{
  [HarmonyPatch(nameof(GhostDrawer.DrawGhostThing))]
  [HarmonyPrefix]
  public static void DrawGhostThing_Prefix(ref AltitudeLayer drawAltitude, ThingDef thingDef)
  {
    if (thingDef == null) return;

    var attachmentType = RoofBuildings.GetAttachmentType(thingDef);
    if (attachmentType == RoofAttachmentType.Rooftop)
    {
      // Force the ghost to be drawn at MetaOverlays (44) which is strictly higher than roofs drawn at MapDataOverlay (43)
      drawAltitude = AltitudeLayer.MetaOverlays;
    }
    else if (attachmentType == RoofAttachmentType.Hanging)
    {
      // Draw hanging ghosts at Silhouettes (42) which is strictly lower than roofs drawn at MapDataOverlay (43)
      drawAltitude = AltitudeLayer.Silhouettes;
    }
  }
}
