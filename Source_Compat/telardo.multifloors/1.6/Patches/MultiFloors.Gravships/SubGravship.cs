using System.Collections.Generic;
using HarmonyLib;
using Verse;
using global::MultiFloors;
using global::MultiFloors.Gravships;

using SolarWeb.Stratum.MapComponents;
using SolarWeb.Stratum.MultiFloors.WorldComponents;
using SolarWeb.Stratum.Stats;
using SolarWeb.Stratum.WorldComponents;

namespace SolarWeb.Stratum.MultiFloors.Patches.MultiFloors.Gravships;

[HarmonyPatch(typeof(SubGravship))]
public static class SubGravship_Patch
{
  /// <summary>
  /// Captures Stratum roof metadata for an upper level as MultiFloors lifts it onto a sub-gravship.
  /// </summary>
  /// <remarks>
  /// MultiFloors records only <c>roofs[cell - origin] = RoofAt(cell)</c>, so stuff, glass tint and
  /// hit points would be lost. Keying off the same <c>cell - origin</c> offset keeps this in step
  /// with MultiFloors' own roof dictionary, which is what the landing pass rotates and replays.
  /// </remarks>
  [HarmonyPatch("CopyCellContents")]
  [HarmonyPostfix]
  public static void CopyCellContents_Postfix(SubGravship __instance, Map oldMap, IntVec3 origin, HashSet<IntVec3> engineFloors)
  {
    if (__instance == null || oldMap == null || engineFloors == null) return;

    // Sub-gravships only ever lift levels away from the ground map, but guard anyway: the ground
    // level is Stratum's own GravshipUtility_Patch territory and restoring it twice would clobber
    // the metadata that path already carries.
    if (oldMap.Level() == 0) return;

    var integrity = oldMap.GetComponent<RoofIntegrityGrid>();
    if (integrity == null) return;

    var constructionTracker = oldMap.GetComponent<RoofConstructionTracker>();
    var retractable = oldMap.GetComponent<RetractableRoofTracker>();

    var roofs = new Dictionary<IntVec3, GravshipRoofTracker.RoofCellData>();
    var construction = new Dictionary<IntVec3, RoofConstructionTracker.ConstructionRecord>();

    foreach (var cell in engineFloors)
    {
      if (!cell.InBounds(oldMap)) continue;

      var local = cell - origin;
      var roof = oldMap.roofGrid.RoofAt(cell);

      if (roof == null)
      {
        // A retracted canopy leaves the roof grid empty, so MultiFloors stores nothing for this
        // cell (and strips null roof entries on save). Capture the stowed roof def as well as its
        // metadata so the canopy comes back with the ship instead of vanishing.
        if (retractable != null &&
            retractable.PeekOpenRoof(oldMap.cellIndices.CellToIndex(cell), out var retractedDef, out var rStuff, out var rTint, out var rHp))
        {
          roofs[local] = new GravshipRoofTracker.RoofCellData
          {
            roofDef = retractedDef,
            stuff = rStuff,
            hitPoints = rHp,
            glassTint = rTint
          };
        }
      }
      else if (RoofStatCache.IsCustomRoof(roof))
      {
        roofs[local] = new GravshipRoofTracker.RoofCellData
        {
          roofDef = roof,
          stuff = integrity.GetStuff(cell),
          hitPoints = integrity.GetHitPoints(cell),
          glassTint = integrity.GetGlassTint(cell)
        };
      }

      if (constructionTracker != null && constructionTracker.TryGetRecord(cell, out var record))
      {
        construction[local] = record;
      }
    }

    if (roofs.Count == 0 && construction.Count == 0) return;

    Find.World?.GetComponent<SubGravshipRoofTracker>()?.Register(__instance.GetUniqueLoadID(), roofs, construction);
  }
}
