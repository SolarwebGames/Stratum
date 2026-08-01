using HarmonyLib;
using Verse;

using SolarWeb.Stratum.Graphics;

namespace SolarWeb.Stratum.Patches;

[HarmonyPatch(typeof(SectionLayer_LightingOverlay))]
public static class SectionLayer_LightingOverlay_Patch
{
  [HarmonyPostfix]
  [HarmonyPatch("GenerateLightingOverlay")]
  public static void GenerateLightingOverlay_Postfix(
    Map map,
    LayerSubMesh subMesh,
    CellRect rect,
    bool centered)
  {
    if (!Stratum.Settings.enableSkylightLighting) return;
    // Baked meshes (gravship capture) have verts offset from world space, so cell lookups would be wrong
    if (centered) return;
    if (map == null || map.roofGrid == null || subMesh?.mesh == null) return;
    if (rect.Width <= 0 || rect.Height <= 0) return;

    SkylightOverlayCompositor.ApplyToLightingOverlay(map, rect, subMesh);
  }
}
