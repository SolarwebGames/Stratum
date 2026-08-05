using HarmonyLib;
using RimWorld;
using Verse;
using global::MultiFloors;

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
    if ((dirtyFlags & (ulong)MapMeshFlagDefOf.Roofs) == 0) return;
    if (___map == null) return;

    // Fast path: one Prepatcher field read on maps with nothing stacked above them.
    Map upper = ___map.UpperMap();
    if (upper == null) return;

    propagating = true;
    try
    {
      // Walk the whole chain, not just one level: a roof on level 0 is visible from level 2 when
      // level 1's terrain is also transparent. Over-dirtying costs a mesh regen, under-dirtying
      // leaves a permanent visual artefact.
      while (upper != null)
      {
        if (loc.InBounds(upper))
        {
          upper.mapDrawer?.MapMeshDirty(loc, (ulong)MapMeshFlagDefOf.Terrain, regenAdjacentCells, regenAdjacentSections);
        }
        upper = upper.UpperMap();
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
    if ((change & (ulong)MapMeshFlagDefOf.Roofs) == 0) return;
    if (___map == null) return;

    Map upper = ___map.UpperMap();
    if (upper == null) return;

    propagating = true;
    try
    {
      while (upper != null)
      {
        upper.mapDrawer?.WholeMapChanged((ulong)MapMeshFlagDefOf.Terrain);
        upper = upper.UpperMap();
      }
    }
    finally
    {
      propagating = false;
    }
  }
}
