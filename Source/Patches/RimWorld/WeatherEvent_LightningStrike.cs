using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

using SolarWeb.Stratum.Stats;
using SolarWeb.Stratum.MapComponents;
using SolarWeb.Stratum.Utilities;

namespace SolarWeb.Stratum.Patches.RimWorld;

[HarmonyPatch(typeof(WeatherEvent_LightningStrike), nameof(WeatherEvent_LightningStrike.DoStrike))]
public static class WeatherEvent_LightningStrike_DoStrike_Patch
{
  [HarmonyPrefix]
  public static bool Prefix(ref IntVec3 strikeLoc, Map map, ref Mesh boltMesh)
  {
    if (!strikeLoc.IsValid)
    {
      strikeLoc = CellFinderLoose.RandomCellWith((IntVec3 sq) => 
        sq.Standable(map) && (!map.roofGrid.Roofed(sq) || !map.roofGrid.RoofAt(sq).isThickRoof), 
        map);
    }

    RoofDef roof = strikeLoc.GetRoof(map);
    if (roof != null && !roof.isThickRoof)
    {
      SoundStarter.PlayOneShotOnCamera(SoundDefOf.Thunder_OffMap, map);
      boltMesh = LightningBoltMeshPool.RandomBoltMesh;

      if (!strikeLoc.Fogged(map))
      {
        GenRoofExplosion.DoExplosion(strikeLoc, map, 1.9f, DamageDefOf.Flame, null, 
          damAmount: DamageDefOf.Flame.defaultDamage, 
          chanceToStartFire: 0.5f);
        
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
