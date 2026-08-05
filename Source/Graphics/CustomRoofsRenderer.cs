using RimWorld;
using UnityEngine;
using Verse;

using SolarWeb.Stratum.Stats;
using SolarWeb.Stratum.MapComponents;

namespace SolarWeb.Stratum.Graphics;

[StaticConstructorOnStartup]
public class CustomRoofsRenderer : SectionLayer
{
  public CustomRoofsRenderer(Section section) : base(section)
  {
    relevantChangeTypes = (ulong)MapMeshFlagDefOf.Roofs | (ulong)MapMeshFlagDefOf.Buildings | (ulong)MapMeshFlagDefOf.FogOfWar;
  }

  // Always return true so the mesh regenerates in the background even if the overlay is hidden
  public override bool Visible => true;

  public override void DrawLayer()
  {
    if (Find.PlaySettings.showRoofOverlay && base.Map == Find.CurrentMap)
    {
      base.DrawLayer();
    }
  }

  public override void Regenerate()
  {
    ClearSubMeshes(MeshParts.All);

    Map map = base.Map;
    if (map == null || map.roofGrid == null || map.fogGrid == null) return;

    var integrityGrid = map.GetComponent<RoofIntegrityGrid>();
    if (integrityGrid != null && !integrityGrid.hasScanned && Visible)
    {
      integrityGrid.ExecuteScan();
    }

    CellRect cellRect = new(section.botLeft.x, section.botLeft.z, 17, 17);
    cellRect.ClipInsideMap(map);

    bool isCutscene = false;
    CellRect captureBounds;
    if (GravshipCapturer.IsGravshipRenderInProgress)
    {
      captureBounds = GravshipCapturer.GravshipCaptureBounds;
    }
    else
    {
      isCutscene = WorldComponent_GravshipController.CutsceneInProgress && !GravshipCapturer.IsGravshipRenderInProgress && map == Find.CurrentMap;
      captureBounds = GravshipCapturer.GravshipCaptureBounds;
    }

    // Use MapDataOverlay to ensure we draw above the lighting overlay, 
    // but leave MetaOverlays available for ghost placement so we don't z-fight.
    float altitude = AltitudeLayer.MapDataOverlay.AltitudeFor();

    foreach (IntVec3 c in cellRect)
    {
      if (map.fogGrid.IsFogged(c)) continue;
      if (isCutscene && captureBounds.Contains(c)) continue;

      RoofDef roof = map.roofGrid.RoofAt(c);
      if (roof == null || !RoofStatCache.IsCustomRoof(roof)) continue;

      // Default options reproduce this renderer's original behaviour: Stratum's own render
      // queues, MetaOverlay for natural roofs, damage scratches on.
      RoofCellPainter.PrintRoofCell(this, map, c, roof, integrityGrid, altitude);
    }


    FinalizeMesh(MeshParts.All);
  }
}
