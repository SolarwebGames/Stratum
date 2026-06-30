using UnityEngine;
using Verse;
using SolarWeb.Stratum.DefModExtensions;

namespace SolarWeb.Stratum.ThingComps;

public class ScannerBooster : ThingComp
{
  private static readonly Color RingColor = new Color(0.3f, 0.8f, 0.5f, 0.5f);

  public override void PostDrawExtraSelectionOverlays()
  {
    var ext = parent.def.GetModExtension<DefModExtensions.ScannerBooster>();
    if (ext != null && ext.range > 0f)
    {
      GenDraw.DrawRadiusRing(parent.Position, ext.range, RingColor);
    }
  }
}
