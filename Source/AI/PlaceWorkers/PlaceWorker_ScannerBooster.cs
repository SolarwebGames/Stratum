using UnityEngine;
using Verse;
using SolarWeb.Stratum.DefModExtensions;

namespace SolarWeb.Stratum.AI.PlaceWorkers;

public class PlaceWorker_ScannerBooster : PlaceWorker
{
  private static readonly Color RingColor = new Color(0.3f, 0.8f, 0.5f, 0.5f);

  public override void DrawGhost(ThingDef def, IntVec3 center, Rot4 rot, Color ghostCol, Thing? thing = null)
  {
    var ext = def.GetModExtension<ScannerBooster>();
    if (ext != null && ext.range > 0f)
    {
      GenDraw.DrawRadiusRing(center, ext.range, RingColor);
    }
  }
}
