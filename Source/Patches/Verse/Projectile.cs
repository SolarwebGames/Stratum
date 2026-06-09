using HarmonyLib;
using SolarWeb.Stratum.DefModExtensions;
using SolarWeb.Stratum.MapComponents;
using Verse;
using Verse.Sound;

namespace SolarWeb.Stratum.Patches;

[HarmonyPatch]
public static class Projectile_Patch
{
  [HarmonyPatch(typeof(Projectile), "ImpactSomething")]
  [HarmonyPrefix]
  public static bool ImpactSomething_Prefix(Projectile __instance)
  {
    if (__instance.def.projectile.flyOverhead)
    {
      var map = __instance.Map;
      var pos = __instance.Position;
      var roof = map.roofGrid.RoofAt(pos);

      if (roof != null && roof.HasModExtension<BuildableRoofExtension>())
      {
        var integrityGrid = map.GetComponent<RoofIntegrityGrid>();
        if (integrityGrid != null)
        {
          int damage = __instance.DamageAmount;

          if (__instance is Projectile_Explosive explosive)
          {
            float radius = explosive.def.projectile.explosionRadius;
            var cells = GenRadial.RadialCellsAround(pos, radius, true);
            foreach (var c in cells)
            {
              if (c.InBounds(map))
              {
                var cRoof = map.roofGrid.RoofAt(c);
                if (cRoof != null && cRoof.HasModExtension<BuildableRoofExtension>())
                {
                  integrityGrid.TakeDamage(c, damage);
                }
              }
            }
          }
          else
          {
            integrityGrid.TakeDamage(pos, damage);
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

