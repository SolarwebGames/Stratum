using HarmonyLib;
using RimWorld;
using Verse;
using global::MultiFloors.Gravships;

using SolarWeb.Stratum.MapComponents;
using SolarWeb.Stratum.MultiFloors.WorldComponents;
using SolarWeb.Stratum.Stats;

namespace SolarWeb.Stratum.MultiFloors.Patches.MultiFloors.Gravships;

[HarmonyPatch(typeof(MF_GravshipUtility))]
public static class MF_GravshipUtility_Patch
{
  /// <summary>
  /// Reapplies Stratum roof metadata after MultiFloors has placed an upper level's roofs.
  /// </summary>
  /// <remarks>
  /// Stratum's vanilla landing path threads metadata through
  /// <c>GravshipPlacementUtility_SpawnRoofs_Patch</c>, whose context is keyed to the ground-level
  /// <c>Gravship</c> and never set for a sub-gravship. Rather than widen that context, this
  /// restores directly here, mirroring what the vanilla postfix does per cell.
  ///
  /// MultiFloors rotates its stored roof offsets with
  /// <c>PrefabUtility.GetAdjustedLocalPosition(local, rotation)</c>, so the same transform is
  /// applied to the captured offsets to keep the two aligned.
  /// </remarks>
  [HarmonyPatch("SpawnRoofs")]
  [HarmonyPostfix]
  public static void SpawnRoofs_Postfix(SubGravship subGravship, Map map, IntVec3 root)
  {
    if (subGravship == null || map == null) return;

    var tracker = Find.World?.GetComponent<SubGravshipRoofTracker>();
    if (tracker == null) return;

    string id = subGravship.GetUniqueLoadID();
    if (!tracker.TryGetRoofData(id, out var data) || data == null) return;

    var integrity = map.GetComponent<RoofIntegrityGrid>();
    var coating = map.GetComponent<SkylightCoating>();
    var constructionTracker = map.GetComponent<RoofConstructionTracker>();
    var rotation = subGravship.Rotation;

    foreach (var kvp in data.roofs)
    {
      var targetCell = root + PrefabUtility.GetAdjustedLocalPosition(kvp.Key, rotation);
      if (!targetCell.InBounds(map)) continue;

      var cellData = kvp.Value;
      var roofDef = cellData.roofDef ?? map.roofGrid.RoofAt(targetCell);
      if (roofDef == null) continue;

      // Retracted canopies were dropped from MultiFloors' roof dictionary (it strips null entries),
      // so place the stowed roof back before restoring its metadata.
      if (map.roofGrid.RoofAt(targetCell) == null)
      {
        map.roofGrid.SetRoof(targetCell, roofDef);
      }

      integrity?.InitializeRoof(
        targetCell,
        roofDef,
        cellData.stuff,
        cellData.glassTint ?? UnityEngine.Color.white,
        cellData.hitPoints >= 0 ? cellData.hitPoints : null);

      coating?.SetSnowLevel(targetCell, 0f);
    }

    if (constructionTracker != null)
    {
      foreach (var kvp in data.construction)
      {
        var targetCell = root + PrefabUtility.GetAdjustedLocalPosition(kvp.Key, rotation);
        if (targetCell.InBounds(map))
        {
          constructionTracker.RestoreRecord(targetCell, kvp.Value);
        }
      }
    }

    tracker.Unregister(id);
  }

  [HarmonyPatch("ShouldRemoveRoof")]
  [HarmonyPrefix]
  public static bool ShouldRemoveRoof_Prefix(Map map, IntVec3 cell, ref bool __result)
  {
    // Only protect roofs Stratum actually owns. MultiFloors' own decision is left intact for
    // every other cell: forcing __result = false unconditionally meant a landing never cleared
    // any roof, including the natural rock the clear step exists to remove.
    if (map == null || !cell.InBounds(map)) return true;

    var roof = map.roofGrid?.RoofAt(cell);
    if (roof != null && RoofStatCache.IsCustomRoof(roof))
    {
      __result = false;
      return false;
    }

    return true;
  }
}
