using System.Collections.Generic;
using Verse;

namespace SolarWeb.Stratum.Explosions;

public static class GenRoofExplosion
{
  public static void DoExplosion(ExplosionConfig config)
  {
    if (config.map == null)
    {
      StratumLog.Warning("Tried to do roof explosion in a null map.");
      return;
    }

    RoofExplosion obj = (RoofExplosion)GenSpawn.Spawn(DefOf.ThingDefOf.RoofExplosion, config.center, config.map);
    obj.Initialize(config);
    obj.StartExplosion(config.explosionSound!, config.ignoredThings!);
  }
}
