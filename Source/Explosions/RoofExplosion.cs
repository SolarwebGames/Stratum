using System;
using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

using SolarWeb.Stratum.Stats;
using SolarWeb.Stratum.MapComponents;
using SolarWeb.Stratum.Utilities;

namespace SolarWeb.Stratum.Explosions;

public class RoofExplosion : Explosion
{
  public void Initialize(ExplosionConfig config)
  {
    radius = config.radius;
    damType = config.damType;
    instigator = config.instigator;

    int resolvedDamAmount = config.damAmount;
    float resolvedArmorPenetration = config.armorPenetration;

    if (resolvedDamAmount < 0)
    {
      resolvedDamAmount = config.damType.defaultDamage;
      resolvedArmorPenetration = config.damType.defaultArmorPenetration;
      if (resolvedDamAmount < 0)
      {
        StratumLog.Error("Attempted to trigger a roof explosion without defined damage");
        resolvedDamAmount = 1;
      }
    }

    if (resolvedArmorPenetration < 0f)
    {
      resolvedArmorPenetration = (float)resolvedDamAmount * 0.015f;
    }

    damAmount = resolvedDamAmount;
    armorPenetration = resolvedArmorPenetration;
    weapon = config.weapon;
    projectile = config.projectile;
    intendedTarget = config.intendedTarget;
    preExplosionSpawnThingDef = config.preExplosionSpawnThingDef;
    preExplosionSpawnChance = config.preExplosionSpawnChance;
    preExplosionSpawnThingCount = config.preExplosionSpawnThingCount;
    postExplosionSpawnThingDef = config.postExplosionSpawnThingDef;
    postExplosionSpawnThingDefWater = config.postExplosionSpawnThingDefWater;
    postExplosionSpawnChance = config.postExplosionSpawnChance;
    postExplosionSpawnThingCount = config.postExplosionSpawnThingCount;
    postExplosionGasType = config.postExplosionGasType;
    postExplosionGasRadiusOverride = config.postExplosionGasRadiusOverride;
    postExplosionGasAmount = config.postExplosionGasAmount;
    applyDamageToExplosionCellsNeighbors = config.applyDamageToExplosionCellsNeighbors;
    chanceToStartFire = config.chanceToStartFire;
    damageFalloff = config.damageFalloff;
    excludeRadius = config.excludeRadius;
    affectedAngle = config.affectedAngle;
    doSoundEffects = config.doSoundEffects;
    screenShakeFactor = config.screenShakeFactor;
    flammabilityChanceCurve = config.flammabilityChanceCurve;
    doVisualEffects = config.doVisualEffects;
    propagationSpeed = config.propagationSpeed;
    overrideCells = config.overrideCells;
    postExplosionSpawnSingleThingDef = config.postExplosionSpawnSingleThingDef;
    preExplosionSpawnSingleThingDef = config.preExplosionSpawnSingleThingDef;
  }

  public override void StartExplosion(SoundDef explosionSound, List<Thing> ignoredThings)
  {
    base.StartExplosion(explosionSound, ignoredThings);

    if (doVisualEffects)
    {
      FleckMaker.Static(Position, Map, DefOf.FleckDefOf.RoofExplosionFlash, radius * 2f);
    }
  }

  public void AffectRoofCell(IntVec3 c)
  {
    if (!c.InBounds(Map) || (excludeRadius > 0f && (float)c.DistanceToSquared(Position) < excludeRadius * excludeRadius))
    {
      return;
    }

    if (doVisualEffects)
    {
      RoofFleckMaker.ThrowSmoke(c.ToVector3Shifted(), Map, Rand.Range(1f, 1.5f), DefOf.FleckDefOf.RoofSmoke);
      if (Rand.Chance(0.3f))
      {
        RoofFleckMaker.ThrowMicroSparks(c.ToVector3Shifted(), Map, DefOf.FleckDefOf.RoofSparks);
      }
    }

    RoofDef roof = c.GetRoof(Map);
    if (roof != null && !roof.isThickRoof && RoofStatCache.IsCustomRoof(roof))
    {
      var integrity = Map.GetComponent<RoofIntegrityGrid>();
      if (integrity != null)
      {
        int damage = GetDamageAmountAt(c);
        integrity.TakeDamage(c, damage, armorPenetration, new DamageInfo(damType, damage));
      }
    }

    if (roof != null && !roof.isThickRoof)
    {
      float num3 = chanceToStartFire;
      if (damageFalloff)
      {
        num3 *= Mathf.Lerp(1f, 0.2f, c.DistanceTo(Position) / radius);
      }
      if (Rand.Chance(num3))
      {
        RoofFireUtility.TryIgniteRoofAt(c, Map, instigator);
      }
    }
  }
}
