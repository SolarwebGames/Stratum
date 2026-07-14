using HarmonyLib;
using RimWorld;
using SolarWeb.Stratum.DefModExtensions;
using SolarWeb.Stratum.MapComponents;
using Verse;

namespace SolarWeb.Stratum.Patches;

[HarmonyPatch]
public static class Skyfaller_Patch
{
  [HarmonyPatch(typeof(Skyfaller), "HitRoof")]
  [HarmonyPrefix]
  public static bool HitRoof_Prefix(Skyfaller __instance)
  {
    if (!Stratum.Settings.enableDropPodInterception) return true;
    var map = __instance.Map;
    if (map == null) return true;

    var integrityGrid = map.GetComponent<RoofIntegrityGrid>();
    if (integrityGrid == null) return true;

    var pos = __instance.Position;
    var skyfallerHealth = __instance.def.BaseMaxHitPoints;

    CellRect cr = __instance.OccupiedRect();
    CellRect cellRect = cr.ExpandedBy((!__instance.def.skyfaller.minimalRoofDestruction) ? 1 : 0).ClipInsideMap(map);

    bool anyBuildableRoof = false;

    foreach (IntVec3 c in cellRect.Cells)
    {
      var roofAtC = map.roofGrid.RoofAt(c);
      if (roofAtC != null && roofAtC.HasModExtension<BuildableRoofExtension>())
      {
        anyBuildableRoof = true;
        integrityGrid.TakeDamage(c, skyfallerHealth);
      }
    }

    if (anyBuildableRoof)
    {
      short hpAfter = integrityGrid.GetHitPoints(pos);

      if (hpAfter > 0)
      {
        for (int i = 0; i < 6; i++)
        {
          FleckMaker.ThrowDustPuff(pos.ToVector3Shifted() + Gen.RandomHorizontalVector(1f), map, 1.2f);
        }
        FleckMaker.ThrowLightningGlow(pos.ToVector3Shifted(), map, 2f);
        GenClamor.DoClamor(__instance, 15f, ClamorDefOf.Impact);

        __instance.Destroy();

        return false;
      }
    }

    return true;
  }
}
