using System.Collections.Generic;
using UnityEngine;
using Verse;
using RimWorld;

namespace SolarWeb.Stratum.ThingComps;

[StaticConstructorOnStartup]
public class PowerPlantSolarRooftop : CompPowerPlantSolar
{
  private static readonly Material PowerPlantSolarBarFilledMat = SolidColorMaterials.SimpleSolidColorMaterial(new Color(0.5f, 0.475f, 0.1f));
  private static readonly Material PowerPlantSolarBarUnfilledMat = SolidColorMaterials.SimpleSolidColorMaterial(new Color(0.15f, 0.15f, 0.15f));

  // Map-wide cache: Key is (map.uniqueID << 32) | def.index. Value stores (tick, calculatedPowerOutput).
  private static readonly Dictionary<long, KeyValuePair<int, float>> globalOutputCache = [];

  private float cachedPowerOutput;

  protected override float DesiredPowerOutput
  {
    get
    {
      if (parent == null || parent.Map == null) return 0f;
      var map = parent.Map;

      long key = ((long)map.uniqueID << 32) | (uint)parent.def.index;
      int currentTick = Find.TickManager.TicksGame;

      if (globalOutputCache.TryGetValue(key, out var cached) && cached.Key == currentTick)
      {
        return cached.Value;
      }

      float value = Mathf.Lerp(0f, 0f - base.Props.PowerConsumption, map.skyManager.CurSkyGlow);
      globalOutputCache[key] = new KeyValuePair<int, float>(currentTick, value);
      return value;
    }
  }

  public override void PostSpawnSetup(bool respawningAfterLoad)
  {
    base.PostSpawnSetup(respawningAfterLoad);
    UpdatePowerOutputForce();
  }

  public override void UpdateDesiredPowerOutput()
  {
    if ((parent.thingIDNumber + Find.TickManager.TicksGame) % 30 == 0)
    {
      UpdatePowerOutputForce();
    }
    else
    {
      base.PowerOutput = cachedPowerOutput;
    }
  }

  private void UpdatePowerOutputForce()
  {
    if ((breakdownableComp != null && breakdownableComp.BrokenDown) || 
        (refuelableComp != null && !refuelableComp.HasFuel) || 
        (flickableComp != null && !flickableComp.SwitchIsOn) || 
        (autoPoweredComp != null && !autoPoweredComp.WantsToBeOn) || 
        (toxifier != null && !toxifier.CanPolluteNow) || 
        !base.PowerOn)
    {
      cachedPowerOutput = 0f;
    }
    else
    {
      cachedPowerOutput = DesiredPowerOutput;
    }
    base.PowerOutput = cachedPowerOutput;
  }

  public override void PostDraw()
  {
    Vector2 drawSize = parent.def.graphicData != null ? parent.def.graphicData.drawSize : Vector2.one;
    Vector2 relativeBarSize = new Vector2(drawSize.x * 0.575f, drawSize.y * 0.035f);

    GenDraw.FillableBarRequest r = new GenDraw.FillableBarRequest
    {
      center = parent.DrawPos + Vector3.up * 0.1f,
      size = relativeBarSize,
      fillPercent = base.PowerOutput / (0f - base.Props.PowerConsumption),
      filledMat = PowerPlantSolarBarFilledMat,
      unfilledMat = PowerPlantSolarBarUnfilledMat,
      margin = 0.15f * (drawSize.x / 4f)
    };

    Rot4 rotation = parent.Rotation;
    rotation.Rotate(RotationDirection.Clockwise);
    r.rotation = rotation;
    GenDraw.DrawFillableBar(r);
  }
}
