using CombatExtended;
using CombatExtended.Compatibility;
using RimWorld;
using Verse;
using Verse.Sound;

using SolarWeb.Stratum.DefModExtensions;
using SolarWeb.Stratum.MapComponents;
using SolarWeb.Stratum.Utilities;

namespace SolarWeb.Stratum.Patches.CE;

[StaticConstructorOnStartup]
public static class CECompatibility
{
  static CECompatibility()
  {
    BlockerRegistry.RegisterImpactSomethingCallback(ImpactSomethingCallback);
  }

  public static bool ImpactSomethingCallback(ProjectileCE projectile, Thing launcher)
  {
    if (projectile.def?.projectile?.flyOverhead ?? false)
    {
      var map = projectile.Map;
      if (map == null) return false;

      var pos = projectile.ExactPosition.ToIntVec3();
      if (!pos.InBounds(map)) return false;

      var roof = map.roofGrid.RoofAt(pos);

      if (roof != null && roof.HasModExtension<BuildableRoofExtension>())
      {
        var integrityGrid = map.GetComponent<RoofIntegrityGrid>();
        if (integrityGrid != null)
        {
          float damage = projectile.DamageAmount;
          float penetration = projectile.PenetrationAmount;
          var damageDef = projectile.def.projectile.damageDef;

          float radius = projectile.def.projectile.explosionRadius;
          if (radius > 0f)
          {
            var cells = GenRadial.RadialCellsAround(pos, radius, true);
            foreach (var c in cells)
            {
              if (c.InBounds(map))
              {
                var cRoof = map.roofGrid.RoofAt(c);
                if (cRoof != null && cRoof.HasModExtension<BuildableRoofExtension>())
                {
                  ApplyCEDamage(integrityGrid, map, c, cRoof, damage, penetration, damageDef, projectile);
                }
              }
            }
          }
          else
          {
            ApplyCEDamage(integrityGrid, map, pos, roof, damage, penetration, damageDef, projectile);
          }

          if (!projectile.def.projectile.soundExplode.NullOrUndefined())
          {
            projectile.def.projectile.soundExplode.PlayOneShot(new TargetInfo(pos, map));
          }

          projectile.Destroy();

          return true; // Return true to tell CE we handled the impact
        }
      }
    }

    return false;
  }

  private static void ApplyCEDamage(RoofIntegrityGrid grid, Map map, IntVec3 cell, RoofDef roof, float damage, float penetration, DamageDef damageDef, Thing instigator)
  {
    grid.TakeDamage(cell, damage, penetration, new DamageInfo(damageDef, damage));
    if (damageDef != null && (damageDef.igniteCellChance > 0f || damageDef == DamageDefOf.Flame || damageDef == DamageDefOf.Burn))
    {
      RoofFireUtility.TryIgniteRoofAt(cell, map, instigator);
    }
  }
}

