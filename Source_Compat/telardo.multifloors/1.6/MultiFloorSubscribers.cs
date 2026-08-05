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
    // Everything below scales its base value by what the levels above transmit, so an unsubscribed
    // or single-level map multiplies by exactly 1 and is indistinguishable from stock Stratum.

    // Drives the ground tint pass, the lighting-overlay brightness, the light pools, the sunbeam
    // flecks and the skylight shadows.
    MapHookRegistry.RegisterGlobal<MapHookRegistry.SkylightTransmissionHandler>(
      MapHookRegistry.HookId.SkylightTransmission,
      (map, cell, baseTransmission) => baseTransmission <= 0f
        ? baseTransmission
        : baseTransmission * SkyOcclusionSampler.Sample(map, cell).Transmission
    );

    // Compounds the colour of every pane between this cell and the sky.
    MapHookRegistry.RegisterGlobal<MapHookRegistry.SkylightTintHandler>(
      MapHookRegistry.HookId.SkylightTint,
      (map, cell, baseTint) => baseTint * SkyOcclusionSampler.Sample(map, cell).Tint
    );

    // The real light level, and solar output. Both were previously all-or-nothing; they now scale
    // with the panes overhead, so a skylight above dims rather than doing nothing.
    MapHookRegistry.RegisterGlobal<MapHookRegistry.SkyGlowMultiplierHandler>(
      MapHookRegistry.HookId.SkyGlowMultiplier,
      (map, cell, baseMultiplier) => baseMultiplier * SkyOcclusionSampler.Sample(map, cell).Transmission
    );

    MapHookRegistry.RegisterGlobal<MapHookRegistry.SolarPowerOutputFactorHandler>(
      MapHookRegistry.HookId.SolarPowerOutputFactor,
      (map, cell, baseFactor) => baseFactor * SkyOcclusionSampler.Sample(map, cell).Transmission
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

  // Light-related occlusion now lives in SkyOcclusionSampler, which reports a scalar and a colour
  // rather than a boolean. This remains only for the placement check, which really is a yes/no
  // question about physical obstruction.
  private static bool IsTransparent(this TerrainDef terrain)
  {
    return terrain != null && global::MultiFloors.MiscDefOfs.MF_UpperLevelSettings.IsTransparentTerrain(terrain);
  }
}
