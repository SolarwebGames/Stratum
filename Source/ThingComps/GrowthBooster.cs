using UnityEngine;
using RimWorld;
using Verse;

namespace SolarWeb.Stratum.ThingComps;

public class GrowthBooster : ThingComp
{
  private CompPowerTrader? powerComp;
  private DefModExtensions.GrowthBooster? cachedProps;

  public DefModExtensions.GrowthBooster? Props => 
    cachedProps ??= parent.def.GetModExtension<DefModExtensions.GrowthBooster>();

  public bool IsActive => (powerComp == null || powerComp.PowerOn) && parent.Spawned;

  public override void PostSpawnSetup(bool respawningAfterLoad)
  {
    base.PostSpawnSetup(respawningAfterLoad);
    powerComp = parent.GetComp<CompPowerTrader>();
    parent.Map.GetComponent<MapComponents.GrowthBoosterTracker>()?.Register(this);
  }

  public override void PostDeSpawn(Map map, DestroyMode mode = DestroyMode.Vanish)
  {
    map.GetComponent<MapComponents.GrowthBoosterTracker>()?.Deregister(this);
    base.PostDeSpawn(map, mode);
  }

  public override void CompTickRare()
  {
    base.CompTickRare();
    if (IsActive && Props != null && Props.emitMist && parent.Map != null)
    {
      if (Rand.Value < 0.3f)
      {
        FleckMaker.ThrowSmoke(parent.DrawPos, parent.Map, Rand.Range(0.6f, 1.0f));
      }
    }
  }

  private static readonly Color RingColor = new Color(0.3f, 0.8f, 0.5f, 0.5f);

  public override void PostDrawExtraSelectionOverlays()
  {
    base.PostDrawExtraSelectionOverlays();
    if (Props != null && Props.radius > 0f)
    {
      GenDraw.DrawRadiusRing(parent.Position, Props.radius, RingColor);
    }
  }
}
