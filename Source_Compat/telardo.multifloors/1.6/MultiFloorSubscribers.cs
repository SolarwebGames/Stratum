using Verse;
using global::MultiFloors;

using SolarWeb.Stratum.DefModExtensions;
using SolarWeb.Stratum.Hooks;
using SolarWeb.Stratum.MapComponents;
using SolarWeb.Stratum.Stats;
using SolarWeb.Stratum.Utilities;

namespace SolarWeb.Stratum.MultiFloors;

internal static class MultiFloorSubscribers
{
  public static void Register()
  {
    // Sky glow and solar output ask the same physical question -- is anything solid stacked above
    // this cell -- so both delegate to the same check.
    MapHookRegistry.RegisterGlobal<MapHookRegistry.SkyGlowMultiplierHandler>(
      MapHookRegistry.HookId.SkyGlowMultiplier,
      (map, cell, baseMultiplier) => IsOccludedFromAbove(map, cell) ? 0f : baseMultiplier
    );

    MapHookRegistry.RegisterGlobal<MapHookRegistry.SolarPowerOutputFactorHandler>(
      MapHookRegistry.HookId.SolarPowerOutputFactor,
      (map, cell, baseFactor) => IsOccludedFromAbove(map, cell) ? 0f : baseFactor
    );

    MapHookRegistry.RegisterGlobal<MapHookRegistry.DropPodRoofInterceptionHandler>(
      MapHookRegistry.HookId.DropPodRoofInterception,
      (Map map, IntVec3 cell, int damageAmount, ref int effectiveHitPoints) =>
      {
        if (map == null || !cell.InBounds(map)) return false;

        Map upper = map.UpperMap();
        if (upper == null) return false;

        Map? highestRoofedMap = null;
        while (upper != null)
        {
          if (cell.InBounds(upper))
          {
            var roof = upper.roofGrid?.RoofAt(cell);
            if (roof != null && roof.HasModExtension<BuildableRoofExtension>())
            {
              highestRoofedMap = upper;
            }
          }
          upper = upper.UpperMap();
        }

        if (highestRoofedMap == null) return false;

        var integrity = highestRoofedMap.GetComponent<RoofIntegrityGrid>();
        if (integrity == null) return false;

        if (damageAmount > 0)
        {
          integrity.TakeDamage(cell, damageAmount);
        }
        effectiveHitPoints = integrity.GetHitPoints(cell);
        return true;
      }
    );

    MapHookRegistry.RegisterGlobal<MapHookRegistry.RoofBuildingPlacementCheckHandler>(
      MapHookRegistry.HookId.RoofBuildingPlacementCheck,
      (checkingDef, loc, rot, map) =>
      {
        if (checkingDef == null || map == null) return null;
        if (RoofBuildings.GetAttachmentType(checkingDef) != RoofAttachmentType.Rooftop) return null;

        Map upper = map.UpperMap();
        if (upper == null) return null;

        // Check the whole footprint, not just the origin: rooftop solar panels and the like are
        // multi-cell, and a panel only partly covered by the floor above is still blocked.
        foreach (IntVec3 cell in GenAdj.OccupiedRect(loc, rot, checkingDef.Size))
        {
          if (!cell.InBounds(upper)) continue;

          var terrain = upper.terrainGrid?.TerrainAt(cell);
          if ((terrain != null && !terrain.IsTransparent()) || cell.GetEdifice(upper) != null)
          {
            return new AcceptanceReport("Stratum_CannotPlaceRooftopUnderUpperLevel".Translate());
          }
        }

        return null;
      }
    );
  }

  /// <summary>
  /// Walks the MultiFloors level stack above <paramref name="cell"/> and reports whether any level
  /// blocks sunlight, either with an opaque roof or with non-transparent floor terrain.
  /// </summary>
  private static bool IsOccludedFromAbove(Map map, IntVec3 cell)
  {
    if (map == null || !cell.InBounds(map)) return false;

    // Fast path for the common single-level case. UpperMap is a Prepatcher field accessor, so
    // this costs one field read on maps that have no level above them.
    Map upper = map.UpperMap();
    if (upper == null) return false;

    while (upper != null)
    {
      if (cell.InBounds(upper))
      {
        var roof = upper.roofGrid?.RoofAt(cell);
        if (roof != null && !RoofStatCache.IsSkylight(roof)) return true;

        var terrain = upper.terrainGrid?.TerrainAt(cell);
        if (terrain != null && !terrain.IsTransparent()) return true;
      }
      upper = upper.UpperMap();
    }

    return false;
  }

  private static bool IsTransparent(this TerrainDef terrain)
  {
    return terrain != null && global::MultiFloors.MiscDefOfs.MF_UpperLevelSettings.IsTransparentTerrain(terrain);
  }
}
