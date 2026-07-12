using RimWorld;
using UnityEngine;
using Verse;

using SolarWeb.Stratum.Utilities;

namespace SolarWeb.Stratum.ThingComps;

public class ScannerBooster : ThingComp
{
  private CompProperties.ScannerBooster? cachedProps;

  public CompProperties.ScannerBooster? Props =>
    cachedProps ??= props as CompProperties.ScannerBooster;

  private static readonly Color RingColor = new(0.3f, 0.8f, 0.5f, 0.5f);

  public override void PostSpawnSetup(bool respawningAfterLoad)
  {
    base.PostSpawnSetup(respawningAfterLoad);
    ScannerBoosterUtility.Register(this);
  }

  public override void PostDeSpawn(Map map, DestroyMode mode = DestroyMode.Vanish)
  {
    base.PostDeSpawn(map, mode);
    ScannerBoosterUtility.Deregister(this);
  }

  public override void PostDrawExtraSelectionOverlays()
  {
    if (Props == null || parent == null || !parent.Spawned || parent.Map == null) return;

    if (Props.range > 0f)
    {
      GenDraw.DrawRadiusRing(parent.Position, Props.range, RingColor);
    }
  }

  public float GetBoosterOffset(IntVec3 origin)
  {
    if (Props == null || parent == null || !parent.Spawned || parent.Map == null) return 0f;

    float maxOffset = 0f;
    var power = parent.TryGetComp<CompPowerTrader>();
    if (power != null && power.PowerOn)
    {
      if ((origin - parent.Position).LengthHorizontal <= Props.range)
      {
        float offset = Props.scanSpeedOffset;
        if (DefOf.StatDefOf.ScanSpeedOffset != null)
        {
          float statVal = parent.GetStatValue(DefOf.StatDefOf.ScanSpeedOffset, true);
          if (statVal > 0f)
          {
            offset = statVal;
          }
        }
        if (offset > maxOffset)
        {
          maxOffset = offset;
        }
      }
    }

    return maxOffset;
  }
}
