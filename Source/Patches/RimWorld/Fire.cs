using HarmonyLib;
using RimWorld;
using Verse;

using SolarWeb.Stratum.Stats;
using SolarWeb.Stratum.MapComponents;
using SolarWeb.Stratum.Things;
using SolarWeb.Stratum.Utilities;

namespace SolarWeb.Stratum.Patches.RimWorld;

[HarmonyPatch(typeof(Fire))]
public static class Fire_Patch
{
  private static readonly AccessTools.FieldRef<Fire, float> fireSizeRef = AccessTools.FieldRefAccess<Fire, float>("fireSize");
  private static readonly AccessTools.FieldRef<Fire, float> flammabilityMaxRef = AccessTools.FieldRefAccess<Fire, float>("flammabilityMax");
  private static readonly AccessTools.FieldRef<Fire, Thing> instigatorRef = AccessTools.FieldRefAccess<Fire, Thing>("instigator");

  [HarmonyPatch("DoComplexCalcs")]
  [HarmonyPostfix]
  public static void DoComplexCalcs_Postfix(Fire __instance)
  {
    if (__instance is not RoofFire && __instance.Spawned && Stratum.Settings.enableRoofFires)
    {
      TryIgniteRoof(__instance);
    }
  }

  [HarmonyPatch("TrySpread")]
  [HarmonyPostfix]
  public static void TrySpread_Postfix(Fire __instance)
  {
    if (!Stratum.Settings.enableRoofFires || !__instance.Spawned) return;

    Map map = __instance.Map;
    if (map == null) return;

    IntVec3 target;
    if (Rand.Chance(0.8f))
    {
      target = __instance.Position + GenRadial.ManualRadialPattern[Rand.RangeInclusive(1, 8)];
    }
    else
    {
      target = __instance.Position + GenRadial.ManualRadialPattern[Rand.RangeInclusive(10, 20)];
    }

    if (!target.InBounds(map)) return;

    RoofDef targetRoof = map.roofGrid.RoofAt(target);
    if (targetRoof != null && RoofStatCache.IsCustomRoof(targetRoof))
    {
      var integrityGrid = map.GetComponent<RoofIntegrityGrid>();
      float flammability = RoofStatCache.GetFlammability(targetRoof, integrityGrid?.GetStuff(target));
      flammability *= fireSizeRef(__instance);

      if (flammability > 0f && !target.ContainsRoofFire(map))
      {
        bool canSpread = true;
        if (target.DistanceToSquared(__instance.Position) > 2)
        {
          CellRect startRect = CellRect.SingleCell(__instance.Position);
          CellRect endRect = CellRect.SingleCell(target);
          canSpread = GenSight.LineOfSight(__instance.Position, target, map, startRect, endRect);
        }

        if (canSpread && Rand.Chance(flammability * 0.25f))
        {
          RoofFireUtility.SpawnRoofFire(target, map, 0.1f, instigatorRef(__instance));
        }
      }
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
}
