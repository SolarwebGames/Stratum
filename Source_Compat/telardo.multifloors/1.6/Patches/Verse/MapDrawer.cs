using HarmonyLib;
using RimWorld;
using Verse;
using global::MultiFloors;

using SolarWeb.Stratum.MultiFloors.MapComponents;

namespace SolarWeb.Stratum.MultiFloors.Patches.Verse;

/// <summary>
/// Propagates roof-mesh invalidation upward through the MultiFloors level stack.
/// </summary>
/// <remarks>
/// MultiFloors renders lower levels through transparent foundations via
/// <c>SectionLayer_LowerLevel</c>, which declares <c>relevantChangeTypes = MapMeshFlagDefOf.Terrain</c>.
/// Every Stratum roof-visual mutation instead dirties <c>MapMeshFlagDefOf.Roofs</c> on the map that
/// owns the roof: <c>RoofGrid.SetRoof</c>, <c>RoofIntegrityGrid</c> (damage, repair, stuff),
/// <c>SkylightCoating</c> (coating, glass tint), and the whole-map invalidations.
///
/// Neither the map nor the flag lines up, and nothing else bridges them, so a roof built or
/// changed on a lower level would keep rendering stale geometry on the level above until the
/// player switched maps and forced a full regeneration.
///
/// Patching the dirty-flag chokepoint rather than subscribing to
/// <c>MapHookRegistry.HookId.RoofChanged</c> is deliberate: that hook only fires from
/// <c>RoofGrid.SetRoof</c> and would miss integrity, damage, coating and glass-tint changes.
/// </remarks>
[HarmonyPatch(typeof(MapDrawer))]
public static class MapDrawer_Patch
{
  private static bool propagating;

  [HarmonyPatch(nameof(MapDrawer.MapMeshDirty), [typeof(IntVec3), typeof(ulong), typeof(bool), typeof(bool)])]
  [HarmonyPostfix]
  public static void MapMeshDirty_Postfix(Map ___map, IntVec3 loc, ulong dirtyFlags, bool regenAdjacentCells, bool regenAdjacentSections)
  {
    if (propagating) return;
    if (___map == null) return;

    bool roofChanged = (dirtyFlags & (ulong)MapMeshFlagDefOf.Roofs) != 0;
    // A floor built on the level above dirties Terrain there, and whether that floor is
    // transparent decides how much light reaches the level below -- so Terrain has to trigger the
    // downward walk even though it never triggers the upward one.
    bool terrainChanged = (dirtyFlags & (ulong)MapMeshFlagDefOf.Terrain) != 0;
    if (!roofChanged && !terrainChanged) return;

    // Fast path: two Prepatcher field reads on maps with nothing stacked around them.
    Map? upper = roofChanged ? ___map.UpperMap() : null;
    Map? lower = ___map.LowerMap();
    if (upper == null && lower == null) return;

    propagating = true;
    try
    {
      // Upward: the lower-level renderer on each map above draws this map's roofs, and it listens
      // for Terrain. Walk the whole chain, not just one level -- a roof on level 0 is visible from
      // level 2 when level 1's terrain is also transparent.
      while (upper != null)
      {
        if (loc.InBounds(upper))
        {
          upper.mapDrawer?.MapMeshDirty(loc, (ulong)MapMeshFlagDefOf.Terrain, regenAdjacentCells, regenAdjacentSections);
        }
        upper = upper.UpperMap();
      }

      // Downward: every level below now derives its skylight brightness and colour from what sits
      // above it, so a roof or floor change here invalidates their cached occlusion and their
      // lighting meshes. Roofs covers all of them -- the vanilla lighting overlay, the tint pass,
      // the light pools, and the shadow renderer via its version bump.
      while (lower != null)
      {
        if (loc.InBounds(lower))
        {
          SkyOcclusionCache.For(lower)?.Invalidate(loc);
          lower.mapDrawer?.MapMeshDirty(loc, (ulong)MapMeshFlagDefOf.Roofs, regenAdjacentCells, regenAdjacentSections);
        }
        lower = lower.LowerMap();
      }
    }
    finally
    {
      propagating = false;
    }
  }

  [HarmonyPatch(nameof(MapDrawer.WholeMapChanged))]
  [HarmonyPostfix]
  public static void WholeMapChanged_Postfix(Map ___map, ulong change)
  {
    if (propagating) return;
    if (___map == null) return;

    bool roofChanged = (change & (ulong)MapMeshFlagDefOf.Roofs) != 0;
    bool terrainChanged = (change & (ulong)MapMeshFlagDefOf.Terrain) != 0;
    if (!roofChanged && !terrainChanged) return;

    Map? upper = roofChanged ? ___map.UpperMap() : null;
    Map? lower = ___map.LowerMap();
    if (upper == null && lower == null) return;

    propagating = true;
    try
    {
      while (upper != null)
      {
        upper.mapDrawer?.WholeMapChanged((ulong)MapMeshFlagDefOf.Terrain);
        upper = upper.UpperMap();
      }

      while (lower != null)
      {
        SkyOcclusionCache.For(lower)?.InvalidateAll();
        lower.mapDrawer?.WholeMapChanged((ulong)MapMeshFlagDefOf.Roofs);
        lower = lower.LowerMap();
      }
    }
    finally
    {
      propagating = false;
    }
  }
}
