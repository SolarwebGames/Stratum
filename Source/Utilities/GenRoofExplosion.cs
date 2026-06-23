using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;
using SolarWeb.Stratum.Things;

namespace SolarWeb.Stratum.Utilities;

public static class GenRoofExplosion
{
  public static void DoExplosion(
    IntVec3 center, Map map, float radius, DamageDef damType, Thing? instigator, 
    int damAmount = -1, float armorPenetration = -1f, SoundDef? explosionSound = null, 
    ThingDef? weapon = null, ThingDef? projectile = null, Thing? intendedTarget = null, 
    ThingDef? postExplosionSpawnThingDef = null, float postExplosionSpawnChance = 0f, 
    int postExplosionSpawnThingCount = 1, GasType? postExplosionGasType = null, 
    float? postExplosionGasRadiusOverride = null, int postExplosionGasAmount = 255, 
    bool applyDamageToExplosionCellsNeighbors = false, ThingDef? preExplosionSpawnThingDef = null, 
    float preExplosionSpawnChance = 0f, int preExplosionSpawnThingCount = 1, 
    float chanceToStartFire = 0f, bool damageFalloff = false, float? direction = null, 
    List<Thing>? ignoredThings = null, FloatRange? affectedAngle = null, 
    bool doVisualEffects = true, float propagationSpeed = 1f, float excludeRadius = 0f, 
    bool doSoundEffects = true, ThingDef? postExplosionSpawnThingDefWater = null, 
    float screenShakeFactor = 1f, SimpleCurve? flammabilityChanceCurve = null, 
    List<IntVec3>? overrideCells = null, ThingDef? postExplosionSpawnSingleThingDef = null, 
    ThingDef? preExplosionSpawnSingleThingDef = null)
  {
    if (map == null)
    {
      StratumLog.Warning("Tried to do roof explosion in a null map.");
      return;
    }

    if (damAmount < 0)
    {
      damAmount = damType.defaultDamage;
      armorPenetration = damType.defaultArmorPenetration;
      if (damAmount < 0)
      {
        StratumLog.Error("Attempted to trigger a roof explosion without defined damage");
        damAmount = 1;
      }
    }

    if (armorPenetration < 0f)
    {
      armorPenetration = (float)damAmount * 0.015f;
    }

    RoofExplosion obj = (RoofExplosion)GenSpawn.Spawn(DefOf.ThingDefOf.RoofExplosion, center, map);
    
    obj.radius = radius;
    obj.damType = damType;
    obj.instigator = instigator;
    obj.damAmount = damAmount;
    obj.armorPenetration = armorPenetration;
    obj.weapon = weapon;
    obj.projectile = projectile;
    obj.intendedTarget = intendedTarget;
    obj.preExplosionSpawnThingDef = preExplosionSpawnThingDef;
    obj.preExplosionSpawnChance = preExplosionSpawnChance;
    obj.preExplosionSpawnThingCount = preExplosionSpawnThingCount;
    obj.postExplosionSpawnThingDef = postExplosionSpawnThingDef;
    obj.postExplosionSpawnThingDefWater = postExplosionSpawnThingDefWater;
    obj.postExplosionSpawnChance = postExplosionSpawnChance;
    obj.postExplosionSpawnThingCount = postExplosionSpawnThingCount;
    obj.postExplosionGasType = postExplosionGasType;
    obj.postExplosionGasRadiusOverride = postExplosionGasRadiusOverride;
    obj.postExplosionGasAmount = postExplosionGasAmount;
    obj.applyDamageToExplosionCellsNeighbors = applyDamageToExplosionCellsNeighbors;
    obj.chanceToStartFire = chanceToStartFire;
    obj.damageFalloff = damageFalloff;
    obj.excludeRadius = excludeRadius;
    obj.affectedAngle = affectedAngle;
    obj.doSoundEffects = doSoundEffects;
    obj.screenShakeFactor = screenShakeFactor;
    obj.flammabilityChanceCurve = flammabilityChanceCurve;
    obj.doVisualEffects = doVisualEffects;
    obj.propagationSpeed = propagationSpeed;
    obj.overrideCells = overrideCells;
    obj.postExplosionSpawnSingleThingDef = postExplosionSpawnSingleThingDef;
    obj.preExplosionSpawnSingleThingDef = preExplosionSpawnSingleThingDef;
    
    obj.StartExplosion(explosionSound!, ignoredThings!);
  }
}
