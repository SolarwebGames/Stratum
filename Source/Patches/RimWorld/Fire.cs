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
