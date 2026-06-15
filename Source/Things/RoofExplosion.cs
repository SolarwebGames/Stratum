using System;
using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

using SolarWeb.Stratum.Stats;
using SolarWeb.Stratum.MapComponents;
using SolarWeb.Stratum.Utilities;

namespace SolarWeb.Stratum.Things;

public class RoofExplosion : Explosion
{
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
        integrity.TakeDamage(c, damage);
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
