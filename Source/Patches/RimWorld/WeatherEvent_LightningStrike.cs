using System.Collections.Generic;
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

      var rodDef = DefOf.ThingDefOf.LightningRod;
      if (rodDef != null && map.listerBuildings != null)
      {
        var rods = map.listerBuildings.AllBuildingsColonistOfDef(rodDef);
        if (rods != null && rods.Count > 0)
        {
          Building? closestRod = null;
          float closestDistance = float.MaxValue;
          for (int i = 0; i < rods.Count; i++)
          {
            if (rods[i] == null) continue;
            float dist = strikeLoc.DistanceTo(rods[i].Position);
            if (dist < closestDistance)
            {
              closestDistance = dist;
              closestRod = rods[i];
            }
          }

          if (closestRod != null && closestDistance <= 20f)
          {
            if (Rand.Value < 0.8f)
            {
              strikeLoc = closestRod.Position;

              float absorbedEnergy = 0f;
              var powerComp = closestRod.GetComp<CompPower>();
              if (powerComp != null && powerComp.PowerNet != null)
              {
                var batteries = powerComp.PowerNet.batteryComps;
                if (batteries != null && batteries.Count > 0)
                {
                  List<CompPowerBattery> validBatteries = new List<CompPowerBattery>();
                  float totalCanAccept = 0f;
                  for (int j = 0; j < batteries.Count; j++)
                  {
                    if (batteries[j] != null)
                    {
                      float accept = batteries[j].AmountCanAccept;
                      if (accept > 0f)
                      {
                        validBatteries.Add(batteries[j]);
                        totalCanAccept += accept;
                      }
                    }
                  }

                  if (validBatteries.Count > 0)
                  {
                    absorbedEnergy = Mathf.Min(1000f, totalCanAccept);
                    float energyPerBat = absorbedEnergy / validBatteries.Count;
                    for (int j = 0; j < validBatteries.Count; j++)
                    {
                      if (validBatteries[j] != null)
                      {
                        validBatteries[j].AddEnergy(energyPerBat);
                      }
                    }
                  }
                }
              }

              if (absorbedEnergy < 1000f)
              {
                GenExplosion.DoExplosion(strikeLoc, map, 1.9f, DamageDefOf.Flame, null);
                closestRod.TakeDamage(new DamageInfo(DamageDefOf.Flame, 60));
              }
              else
              {
                closestRod.TakeDamage(new DamageInfo(DamageDefOf.Flame, 20));
              }

              boltMesh = LightningBoltMeshPool.RandomBoltMesh;
              SoundInfo info = SoundInfo.InMap(new TargetInfo(strikeLoc, map));
              if (SoundDefOf.Thunder_OnMap != null)
              {
                SoundDefOf.Thunder_OnMap.PlayOneShot(info);
              }

              Vector3 loc = strikeLoc.ToVector3Shifted();
              for (int i = 0; i < 6; i++)
              {
                FleckMaker.ThrowMicroSparks(loc, map);
                FleckMaker.ThrowLightningGlow(loc, map, 2.0f);
              }

              return false;
            }
          }
        }
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
