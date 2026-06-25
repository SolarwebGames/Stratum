using RimWorld;
using UnityEngine;
using Verse;
using SolarWeb.Stratum.Stats;
using SolarWeb.Stratum.Graphics;

namespace SolarWeb.Stratum.MapComponents;

[StaticConstructorOnStartup]
public class RoofDesignationOverlay(Map map) : MapComponent(map)
{
  private static Texture2D? deconstructIcon;
  private static Texture2D DeconstructIcon => deconstructIcon ??= ContentFinder<Texture2D>.Get(RimWorldTextures.UI.Designators.Deconstruct);

  public override void MapComponentOnGUI()
  {
    if (!Find.PlaySettings.showRoofOverlay) return;
    if (map.areaManager == null || map.roofGrid == null) return;

    var noRoof = map.areaManager.NoRoof;
    var roofGrid = map.roofGrid;
    var viewRect = Find.CameraDriver.CurrentViewRect;

    foreach (IntVec3 c in viewRect)
    {
      if (c.InBounds(map) && noRoof[c])
      {
        RoofDef roof = roofGrid.RoofAt(c);
        if (RoofStatCache.IsCustomRoof(roof))
        {
          Vector2 screenPos = GenMapUI.LabelDrawPosFor(c);
          Rect rect = new(screenPos.x - 16f, screenPos.y - 16f, 32f, 32f);

          Widgets.DrawTextureFitted(rect, DeconstructIcon, 0.85f, 0.25f);
        }
      }
    }
  }
}
