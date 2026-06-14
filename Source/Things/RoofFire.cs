using UnityEngine;
using Verse;
using RimWorld;

namespace SolarWeb.Stratum.Things;

public class RoofFire : Fire
{
  public override Vector3 DrawPos
  {
    get
    {
      Vector3 pos = base.DrawPos;
      pos.y = AltitudeLayer.MoteOverhead.AltitudeFor() + 0.1f;
      return pos;
    }
  }
}
