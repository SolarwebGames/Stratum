using HarmonyLib;
using UnityEngine;
using RimWorld;
using Verse;
using Verse.Sound;

using SolarWeb.Stratum.Stats;
using SolarWeb.Stratum.MapComponents;
using SolarWeb.Stratum.Utilities;

namespace SolarWeb.Stratum.Things;

public class RoofFire : Fire
{
  private static readonly AccessTools.FieldRef<Fire, float> fireSizeRef = AccessTools.FieldRefAccess<Fire, float>("fireSize");
  private static readonly AccessTools.FieldRef<Fire, float> flammabilityMaxRef = AccessTools.FieldRefAccess<Fire, float>("flammabilityMax");
  private static readonly AccessTools.FieldRef<Fire, Thing> instigatorRef = AccessTools.FieldRefAccess<Fire, Thing>("instigator");
  private static readonly AccessTools.FieldRef<Fire, int> ticksSinceSpawnRef = AccessTools.FieldRefAccess<Fire, int>("ticksSinceSpawn");
  private static readonly AccessTools.FieldRef<Fire, Sustainer> sustainerRef = AccessTools.FieldRefAccess<Fire, Sustainer>("sustainer");

  public override Vector3 DrawPos
  {
    get
    {
      Vector3 pos = base.DrawPos;
      pos.y = AltitudeLayer.MoteOverhead.AltitudeFor() + 0.1f;
      return pos;
    }
  }

  protected override void TickInterval(int delta)
  {
    int ticksSinceSpawn = ticksSinceSpawnRef(this);
    ticksSinceSpawn += delta;
    ticksSinceSpawnRef(this) = ticksSinceSpawn;

    if (ticksSinceSpawn >= 150)
    {
      DoRoofFireComplexCalcs();
      ticksSinceSpawnRef(this) = 0;
    }

    if (ticksSinceSpawn % 150 == 100 && Stratum.Settings.enableRoofFires)
    {
      TrySpreadRoofFire();
    }

    if (Spawned)
    {
      sustainerRef(this)?.Maintain();

      if (Rand.Chance(0.02f * delta * fireSizeRef(this)))
      {
        FleckMaker.ThrowSmoke(DrawPos, Map, fireSizeRef(this));
      }
      if (fireSizeRef(this) > 0.7f && Rand.Chance(0.01f * delta * fireSizeRef(this)))
      {
        FleckMaker.ThrowMicroSparks(DrawPos, Map);
      }
    }
  }

  private void DoRoofFireComplexCalcs()
  {
    Map map = Map;
    IntVec3 pos = Position;
    RoofDef roof = map.roofGrid.RoofAt(pos);

    if (roof == null || !RoofStatCache.IsCustomRoof(roof))
    {
      Destroy();
      return;
    }

    var integrityGrid = map.GetComponent<RoofIntegrityGrid>();
    ThingDef? stuff = integrityGrid?.GetStuff(pos);
    float flammability = RoofStatCache.GetFlammability(roof, stuff);

    if (flammability < 0.01f)
    {
      Destroy();
      return;
    }

    flammabilityMaxRef(this) = flammability;

    float fireSize = fireSizeRef(this);
    int damage = GenMath.RoundRandom(Mathf.Clamp(0.0125f + 0.0036f * fireSize, 0.0125f, 0.05f) * 150f);
    if (damage < 1) damage = 1;

    integrityGrid?.TakeDamage(pos, damage);

    if (!Spawned) return;

    GenTemperature.PushHeat(pos, map, fireSize * 160f);

    float effectiveVacuum = FireUtility.GetEffectiveVacuumForFire(pos, map);
    fireSize += 0.00055f * flammability * 150f * (1f - effectiveVacuum);
    if (fireSize > 1.75f) fireSize = 1.75f;
    fireSizeRef(this) = fireSize;

    if (map.weatherManager.RainRate > 0.01f && !roof.isThickRoof)
    {
      TakeDamage(new DamageInfo(DamageDefOf.Extinguish, 10f));
    }

    if (effectiveVacuum > 0f)
    {
      TakeDamage(new DamageInfo(DamageDefOf.Extinguish, 20f * effectiveVacuum));
    }
  }

  private void TrySpreadRoofFire()
  {
    IntVec3 pos = Position;
    Map map = Map;

    IntVec3 target = pos + GenAdj.AdjacentCells[Rand.RangeInclusive(0, 7)];
    if (!target.InBounds(map)) return;

    RoofDef targetRoof = map.roofGrid.RoofAt(target);
    if (targetRoof != null && RoofStatCache.IsCustomRoof(targetRoof))
    {
      var integrityGrid = map.GetComponent<RoofIntegrityGrid>();
      float flammability = RoofStatCache.GetFlammability(targetRoof, integrityGrid?.GetStuff(target));
      flammability *= fireSizeRef(this);

      if (flammability > 0f && !target.ContainsRoofFire(map))
      {
        if (Rand.Value < flammability * 0.5f)
        {
          RoofFireUtility.SpawnRoofFire(target, map, 0.1f, instigatorRef(this));
        }
      }
    }
  }
}
