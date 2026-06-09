using HarmonyLib;
using RimWorld;
using SolarWeb.Stratum.DefModExtensions;
using SolarWeb.Stratum.MapComponents;
using Verse;

namespace SolarWeb.Stratum.Patches;

[HarmonyPatch]
public static class DropPodUtility_Patch
{
  [HarmonyPatch(typeof(DropPodUtility), nameof(DropPodUtility.MakeDropPodAt))]
  [HarmonyPrefix]
  public static void MakeDropPodAt_Prefix(ref IntVec3 c, Map map, ActiveTransporterInfo info, Faction faction)
  {
    if (map == null || !c.IsValid || !c.InBounds(map)) return;

    var roof = map.roofGrid.RoofAt(c);
    if (roof != null && roof.HasModExtension<BuildableRoofExtension>())
    {
      var integrityGrid = map.GetComponent<RoofIntegrityGrid>();
      if (integrityGrid != null)
      {
        var fallerDef = info?.sentTransporterDef?.dropPodFaller ?? faction?.def.dropPodIncoming ?? ThingDefOf.DropPodIncoming;
        var podDamage = (fallerDef.BaseMaxHitPoints < 300) ? 300 : fallerDef.BaseMaxHitPoints;

        if (integrityGrid.GetHitPoints(c) > podDamage)
        {
          IntVec3 originalCell = c;
          IntVec3 newCell = IntVec3.Invalid;

          int searchRadius = 15;
          int maxCells = GenRadial.NumCellsInRadius(searchRadius);
          for (int i = 0; i < maxCells; i++)
          {
            IntVec3 searchCell = originalCell + GenRadial.RadialPattern[i];
            if (searchCell.InBounds(map) && searchCell.Walkable(map))
            {
              var searchRoof = map.roofGrid.RoofAt(searchCell);
              bool isBlocked = false;
              if (searchRoof != null && searchRoof.HasModExtension<BuildableRoofExtension>())
              {
                if (integrityGrid.GetHitPoints(searchCell) > podDamage)
                {
                  isBlocked = true;
                }
              }

              if (!isBlocked)
              {
                newCell = searchCell;
                break;
              }
            }
          }

          if (newCell.IsValid)
          {
            c = newCell;

            var podDef = info?.sentTransporterDef?.dropPodActive ?? faction?.def.dropPodActive ?? ThingDefOf.ActiveDropPod;
            ActiveTransporter dummyTransporter = (ActiveTransporter)ThingMaker.MakeThing(podDef);
            dummyTransporter.Contents = new ActiveTransporterInfo();
            SkyfallerMaker.SpawnSkyfaller(fallerDef, dummyTransporter, originalCell, map);
          }
        }
      }
    }
  }
}
