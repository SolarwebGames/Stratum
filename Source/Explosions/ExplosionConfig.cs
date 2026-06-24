using System.Collections.Generic;
using Verse;

namespace SolarWeb.Stratum.Explosions;

public struct ExplosionConfig
{
  public IntVec3 center;
  public Map map;
  public float radius;
  public DamageDef damType;
  public Thing? instigator;
  public int damAmount;
  public float armorPenetration;
  public SoundDef? explosionSound;
  public ThingDef? weapon;
  public ThingDef? projectile;
  public Thing? intendedTarget;
  public ThingDef? postExplosionSpawnThingDef;
  public float postExplosionSpawnChance;
  public int postExplosionSpawnThingCount;
  public GasType? postExplosionGasType;
  public float? postExplosionGasRadiusOverride;
  public int postExplosionGasAmount;
  public bool applyDamageToExplosionCellsNeighbors;
  public ThingDef? preExplosionSpawnThingDef;
  public float preExplosionSpawnChance;
  public int preExplosionSpawnThingCount;
  public float chanceToStartFire;
  public bool damageFalloff;
  public float? direction;
  public List<Thing>? ignoredThings;
  public FloatRange? affectedAngle;
  public bool doVisualEffects;
  public float propagationSpeed;
  public float excludeRadius;
  public bool doSoundEffects;
  public ThingDef? postExplosionSpawnThingDefWater;
  public float screenShakeFactor;
  public SimpleCurve? flammabilityChanceCurve;
  public List<IntVec3>? overrideCells;
  public ThingDef? postExplosionSpawnSingleThingDef;
  public ThingDef? preExplosionSpawnSingleThingDef;

  public ExplosionConfig()
  {
    center = default;
    map = null!;
    radius = default;
    damType = null!;
    instigator = null;
    damAmount = -1;
    armorPenetration = -1f;
    explosionSound = null;
    weapon = null;
    projectile = null;
    intendedTarget = null;
    postExplosionSpawnThingDef = null;
    postExplosionSpawnChance = 0f;
    postExplosionSpawnThingCount = 1;
    postExplosionGasType = null;
    postExplosionGasRadiusOverride = null;
    postExplosionGasAmount = 255;
    applyDamageToExplosionCellsNeighbors = false;
    preExplosionSpawnThingDef = null;
    preExplosionSpawnChance = 0f;
    preExplosionSpawnThingCount = 1;
    chanceToStartFire = 0f;
    damageFalloff = false;
    direction = null;
    ignoredThings = null;
    affectedAngle = null;
    doVisualEffects = true;
    propagationSpeed = 1f;
    excludeRadius = 0f;
    doSoundEffects = true;
    postExplosionSpawnThingDefWater = null;
    screenShakeFactor = 1f;
    flammabilityChanceCurve = null;
    overrideCells = null;
    postExplosionSpawnSingleThingDef = null;
    preExplosionSpawnSingleThingDef = null;
  }
}