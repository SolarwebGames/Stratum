using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using Verse;

using SolarWeb.Stratum.Stats;
using SolarWeb.Stratum.Things;

namespace SolarWeb.Stratum.Utilities;

public static class RoofFireUtility
{
  private static readonly AccessTools.FieldRef<Fire, float> fireSizeRef = AccessTools.FieldRefAccess<Fire, float>("fireSize");
  private static readonly AccessTools.FieldRef<Fire, Thing> instigatorRef = AccessTools.FieldRefAccess<Fire, Thing>("instigator");

  public static void SpawnRoofFire(IntVec3 c, Map map, float size, Thing instigator)
  {
    if (c.ContainsRoofFire(map)) return;

    RoofFire fire = (RoofFire)ThingMaker.MakeThing(DefOf.ThingDefOf.RoofFire);
    fireSizeRef(fire) = size;
    instigatorRef(fire) = instigator;
    GenSpawn.Spawn(fire, c, map, Rot4.North);
  }

  public static bool ContainsRoofFire(this IntVec3 c, Map map)
  {
    if (map?.thingGrid == null) return false;
    List<Thing> list = map.thingGrid.ThingsListAt(c);
    for (int i = 0; i < list.Count; i++)
    {
      if (list[i] is RoofFire) return true;
    }
    return false;
  }

  public static void TryIgniteRoofAt(IntVec3 c, Map map, Thing instigator, DamageDef? damageDef = null)
  {
    RoofDef roof = c.GetRoof(map);
    if (roof != null && RoofStatCache.IsCustomRoof(roof))
    {
      var integrity = map.GetComponent<MapComponents.RoofIntegrityGrid>();
      float flammability = RoofStatCache.GetFlammability(roof, integrity?.GetStuff(c));

      if (flammability > 0f)
      {
        float ignitionChance = flammability;
        if (damageDef != null)
        {
          ignitionChance *= damageDef.igniteCellChance;
        }

        if (Rand.Value < ignitionChance)
        {
          SpawnRoofFire(c, map, 0.1f, instigator);
        }
      }
    }
  }
}
