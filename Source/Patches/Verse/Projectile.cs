using HarmonyLib;
using RimWorld;
using Verse;
using Verse.Sound;

using SolarWeb.Stratum.DefModExtensions;
using SolarWeb.Stratum.MapComponents;
using SolarWeb.Stratum.Utilities;
using SolarWeb.Stratum.Explosions;

namespace SolarWeb.Stratum.Patches;

[HarmonyPatch(typeof(Projectile))]
public static class Projectile_Patch
{
  private static readonly AccessTools.FieldRef<Projectile, ThingDef> equipmentDefRef = AccessTools.FieldRefAccess<Projectile, ThingDef>("equipmentDef");

  [HarmonyPatch("ImpactSomething")]
  [HarmonyPrefix]
  public static bool ImpactSomething_Prefix(Projectile __instance)
  {
    if (__instance.def.projectile.flyOverhead)
    {
      var map = __instance.Map;
      if (map == null || map.roofGrid == null) return true;
      var pos = __instance.Position;
      var roof = map.roofGrid.RoofAt(pos);

      if (roof != null && roof.HasModExtension<BuildableRoofExtension>())
      {
        var integrityGrid = map.GetComponent<RoofIntegrityGrid>();
        if (integrityGrid != null)
        {
          int damage = __instance.DamageAmount;
          var damageDef = __instance.def.projectile.damageDef;

          if (__instance is Projectile_Explosive explosive)
          {
            float radius = explosive.def.projectile.explosionRadius;
            GenRoofExplosion.DoExplosion(new ExplosionConfig
            {
              center = pos,
              map = map,
              radius = radius,
              damType = damageDef,
              instigator = __instance,
              damAmount = damage,
              armorPenetration = __instance.ArmorPenetration,
              explosionSound = __instance.def.projectile.soundExplode,
              weapon = equipmentDefRef(__instance),
              projectile = __instance.def,
              intendedTarget = __instance.intendedTarget.Thing,
              postExplosionSpawnThingDef = __instance.def.projectile.postExplosionSpawnThingDef,
              postExplosionSpawnChance = __instance.def.projectile.postExplosionSpawnChance,
              postExplosionSpawnThingCount = __instance.def.projectile.postExplosionSpawnThingCount,
              postExplosionGasType = __instance.def.projectile.postExplosionGasType,
              postExplosionGasRadiusOverride = null,
              applyDamageToExplosionCellsNeighbors = __instance.def.projectile.applyDamageToExplosionCellsNeighbors,
              preExplosionSpawnThingDef = __instance.def.projectile.preExplosionSpawnThingDef,
              preExplosionSpawnChance = __instance.def.projectile.preExplosionSpawnChance,
              preExplosionSpawnThingCount = __instance.def.projectile.preExplosionSpawnThingCount,
              chanceToStartFire = __instance.def.projectile.explosionChanceToStartFire,
              damageFalloff = __instance.def.projectile.explosionDamageFalloff
            });
          }
          else
          {
            integrityGrid.TakeDamage(pos, damage, __instance.ArmorPenetration, new DamageInfo(damageDef, damage));
            if (damageDef != null && (damageDef.igniteCellChance > 0f || damageDef == DamageDefOf.Flame || damageDef == DamageDefOf.Burn))
            {
              RoofFireUtility.TryIgniteRoofAt(pos, map, __instance, damageDef);
            }
          }

          if (!__instance.def.projectile.soundExplode.NullOrUndefined())
          {
            __instance.def.projectile.soundExplode.PlayOneShot(new TargetInfo(pos, map));
          }
          __instance.Destroy();

          return false;
        }
      }
    }

    return true;
  }
}

