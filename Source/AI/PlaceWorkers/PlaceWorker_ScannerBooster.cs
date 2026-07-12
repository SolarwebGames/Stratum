using UnityEngine;
using Verse;
using SolarWeb.Stratum.CompProperties;

namespace SolarWeb.Stratum.AI.PlaceWorkers;

public class PlaceWorker_ScannerBooster : PlaceWorker
{
  private static readonly Color RingColor = new Color(0.3f, 0.8f, 0.5f, 0.5f);

  public override void DrawGhost(ThingDef def, IntVec3 center, Rot4 rot, Color ghostCol, Thing? thing = null)
  {
    var props = def.GetCompProperties<ScannerBooster>();
    if (props != null && props.range > 0f)
    {
      GenDraw.DrawRadiusRing(center, props.range, RingColor);
    }
  }
}
