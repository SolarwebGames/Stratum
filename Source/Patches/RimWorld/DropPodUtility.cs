using HarmonyLib;
using RimWorld;
using Verse;

using SolarWeb.Stratum.DefModExtensions;
using SolarWeb.Stratum.MapComponents;

namespace SolarWeb.Stratum.Patches;

[HarmonyPatch]
public static class DropPodUtility_Patch
{
  [HarmonyPatch(typeof(DropPodUtility), nameof(DropPodUtility.MakeDropPodAt))]
  [HarmonyPrefix]
  public static void MakeDropPodAt_Prefix(ref IntVec3 c, Map map, ActiveTransporterInfo info, Faction faction)
  {
    if (!Stratum.Settings.enableDropPodInterception) return;
    if (map == null || !c.IsValid || !c.InBounds(map)) return;

    RoofIntegrityGrid? integrityGrid = null;
    bool intercepted = TryGetRoofProtection(map, c, ref integrityGrid, out int effectiveHp);

    if (intercepted)
    {
      var fallerDef = info?.sentTransporterDef?.dropPodFaller ?? faction?.def.dropPodIncoming ?? ThingDefOf.DropPodIncoming;
      var podDamage = (fallerDef.BaseMaxHitPoints < 300) ? 300 : fallerDef.BaseMaxHitPoints;

      if (effectiveHp > podDamage)
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
            bool isBlocked = TryGetRoofProtection(map, searchCell, ref integrityGrid, out int searchHp)
                             && searchHp > podDamage;

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

  /// <summary>
  /// Reports the strongest roof shielding <paramref name="cell"/>, combining this map's own roof
  /// with anything a compat hook contributes from other levels.
  /// </summary>
  /// <remarks>
  /// Both sources are always evaluated and the higher hit point value wins. Consulting only one of
  /// them would let a flimsy roof on one level mask a reinforced roof on another -- e.g. a cheap
  /// upper floor reporting low hit points would leave a reinforced roof below it never checked,
  /// silently disabling interception.
  /// </remarks>
  private static bool TryGetRoofProtection(Map map, IntVec3 cell, ref RoofIntegrityGrid? integrityGrid, out int effectiveHitPoints)
  {
    effectiveHitPoints = 0;
    bool any = false;

    // Damage is only applied by Skyfaller_Patch on actual impact; this is a query.
    int hookHp = 0;
    if (Hooks.MapHookRegistry.InterceptDropPodByRoof(map, cell, 0, ref hookHp))
    {
      effectiveHitPoints = hookHp;
      any = true;
    }

    var roof = map.roofGrid.RoofAt(cell);
    if (roof != null && roof.HasModExtension<BuildableRoofExtension>())
    {
      integrityGrid ??= map.GetComponent<RoofIntegrityGrid>();
      if (integrityGrid != null)
      {
        int localHp = integrityGrid.GetHitPoints(cell);
        if (!any || localHp > effectiveHitPoints) effectiveHitPoints = localHp;
        any = true;
      }
    }

    return any;
  }
}
