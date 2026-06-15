using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using Verse;
using UnityEngine;

using SolarWeb.Stratum.Stats;
using SolarWeb.Stratum.MapComponents;
using SolarWeb.Stratum.Things;
using SolarWeb.Stratum.Utilities;

namespace SolarWeb.Stratum.Patches.RimWorld;

[HarmonyPatch]
public static class Fire_Patch
{
  private static readonly AccessTools.FieldRef<Fire, float> fireSizeRef = AccessTools.FieldRefAccess<Fire, float>("fireSize");
  private static readonly AccessTools.FieldRef<Fire, float> flammabilityMaxRef = AccessTools.FieldRefAccess<Fire, float>("flammabilityMax");
  private static readonly AccessTools.FieldRef<Fire, Thing> instigatorRef = AccessTools.FieldRefAccess<Fire, Thing>("instigator");

  [HarmonyPatch(typeof(Fire), "DoComplexCalcs")]
  [HarmonyPrefix]
  public static bool DoComplexCalcs_Prefix(Fire __instance)
  {
    if (__instance is RoofFire)
    {
      DoRoofFireComplexCalcs(__instance);
      return false;
    }
    return true;
  }

  [HarmonyPatch(typeof(Fire), "DoComplexCalcs")]
  [HarmonyPostfix]
  public static void DoComplexCalcs_Postfix(Fire __instance)
  {
    if (__instance is not RoofFire && __instance.Spawned && Stratum.Settings.enableRoofFires)
    {
      TryIgniteRoof(__instance);
    }
  }

  [HarmonyPatch(typeof(Fire), "TrySpread")]
  [HarmonyPrefix]
  public static bool TrySpread_Prefix(Fire __instance)
  {
    if (__instance is RoofFire && Stratum.Settings.enableRoofFires)
    {
      TrySpreadRoofFire(__instance);
      return false;
    }
    return true;
  }

  private static void DoRoofFireComplexCalcs(Fire fire)
  {
    Map map = fire.Map;
    IntVec3 pos = fire.Position;
    RoofDef roof = map.roofGrid.RoofAt(pos);

    if (roof == null || !RoofStatCache.IsCustomRoof(roof))
    {
      fire.Destroy();
      return;
    }

    var integrityGrid = map.GetComponent<RoofIntegrityGrid>();
    ThingDef? stuff = integrityGrid?.GetStuff(pos);
    float flammability = RoofStatCache.GetFlammability(roof, stuff);

    if (flammability < 0.01f)
    {
      fire.Destroy();
      return;
    }

    flammabilityMaxRef(fire) = flammability;

    float fireSize = fireSizeRef(fire);
    int damage = GenMath.RoundRandom(Mathf.Clamp(0.0125f + 0.0036f * fireSize, 0.0125f, 0.05f) * 150f);
    if (damage < 1) damage = 1;

    integrityGrid?.TakeDamage(pos, damage);

    if (!fire.Spawned) return;

    GenTemperature.PushHeat(pos, map, fireSize * 160f);

    float effectiveVacuum = FireUtility.GetEffectiveVacuumForFire(pos, map);
    fireSize += 0.00055f * flammability * 150f * (1f - effectiveVacuum);
    if (fireSize > 1.75f) fireSize = 1.75f;
    fireSizeRef(fire) = fireSize;

    if (map.weatherManager.RainRate > 0.01f && !roof.isThickRoof)
    {
      fire.TakeDamage(new DamageInfo(DamageDefOf.Extinguish, 10f));
    }

    if (effectiveVacuum > 0f)
    {
      fire.TakeDamage(new DamageInfo(DamageDefOf.Extinguish, 20f * effectiveVacuum));
    }
  }

  private static void TryIgniteRoof(Fire groundFire)
  {
    Map map = groundFire.Map;
    IntVec3 pos = groundFire.Position;
    RoofDef roof = map.roofGrid.RoofAt(pos);

    if (roof != null && RoofStatCache.IsCustomRoof(roof))
    {
      var integrityGrid = map.GetComponent<RoofIntegrityGrid>();
      float flammability = RoofStatCache.GetFlammability(roof, integrityGrid?.GetStuff(pos));

      if (flammability > 0f)
      {
        if (!pos.ContainsRoofFire(map))
        {
          float chance = groundFire.fireSize * flammability * 0.5f;
          if (Rand.Value < chance)
          {
            RoofFireUtility.SpawnRoofFire(pos, map, 0.1f, instigatorRef(groundFire));
          }
        }
      }
    }
  }

  private static void TrySpreadRoofFire(Fire fire)
  {
    IntVec3 pos = fire.Position;
    Map map = fire.Map;

    IntVec3 target = pos + GenAdj.AdjacentCells[Rand.RangeInclusive(0, 7)];
    if (!target.InBounds(map)) return;

    RoofDef targetRoof = map.roofGrid.RoofAt(target);
    if (targetRoof != null && RoofStatCache.IsCustomRoof(targetRoof))
    {
      var integrityGrid = map.GetComponent<RoofIntegrityGrid>();
      float flammability = RoofStatCache.GetFlammability(targetRoof, integrityGrid?.GetStuff(target));
      flammability *= fire.fireSize;

      if (flammability > 0f && !target.ContainsRoofFire(map))
      {
        if (Rand.Value < flammability * 0.5f)
        {
          RoofFireUtility.SpawnRoofFire(target, map, 0.1f, instigatorRef(fire));
        }
      }
    }
  }
}
