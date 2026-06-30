using HarmonyLib;
using UnityEngine;
using Verse;
using Verse.Sound;
using RimWorld;

using SolarWeb.Stratum.Explosions;
using SolarWeb.Stratum.Utilities;

namespace SolarWeb.Stratum.Patches;

[HarmonyPatch(typeof(WeatherEvent_LightningStrike))]
public static class WeatherEvent_LightningStrike_Patch
{
  [HarmonyPatch(nameof(WeatherEvent_LightningStrike.DoStrike))]
  [HarmonyPrefix]
  public static bool DoStrike_Prefix(ref IntVec3 strikeLoc, Map map, ref Mesh boltMesh)
  {
    if (map == null) return true;

    try
    {
      if (!strikeLoc.IsValid)
      {
        strikeLoc = CellFinderLoose.RandomCellWith((IntVec3 sq) =>
          sq.Standable(map) && (map.roofGrid == null || !map.roofGrid.Roofed(sq) || map.roofGrid.RoofAt(sq) == null || !map.roofGrid.RoofAt(sq).isThickRoof),
          map);
      }

    }
    catch (System.Exception ex)
    {
      StratumLog.Error($"Error in LightningStrike DoStrike_Prefix: {ex}");
    }

    RoofDef roof = strikeLoc.GetRoof(map);
    if (roof != null && !roof.isThickRoof)
    {
      SoundStarter.PlayOneShotOnCamera(SoundDefOf.Thunder_OffMap, map);
      boltMesh = LightningBoltMeshPool.RandomBoltMesh;

      if (!strikeLoc.Fogged(map))
      {
        GenRoofExplosion.DoExplosion(new ExplosionConfig
        {
          center = strikeLoc,
          map = map,
          radius = 1.9f,
          damType = DamageDefOf.Flame,
          instigator = null,
          damAmount = 50,
          chanceToStartFire = 0.5f
        });

        for (int i = 0; i < 4; i++)
        {
          RoofFleckMaker.ThrowSmoke(strikeLoc.ToVector3Shifted(), map, 1.5f, DefOf.FleckDefOf.RoofSmoke);
          RoofFleckMaker.ThrowMicroSparks(strikeLoc.ToVector3Shifted(), map, DefOf.FleckDefOf.RoofSparks);
          FleckMaker.ThrowLightningGlow(strikeLoc.ToVector3Shifted(), map, 1.5f);
        }
      }

      SoundStarter.PlayOneShot(SoundDefOf.Thunder_OnMap, SoundInfo.InMap(new TargetInfo(strikeLoc, map)));
      return false;
    }

    return true;
  }
}
