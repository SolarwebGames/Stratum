using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using SolarWeb.Stratum.DefModExtensions;
using SolarWeb.Stratum.MapComponents;
using Verse;

namespace SolarWeb.Stratum.Patches;

/// <summary>
/// After a StructureLayout spawns, replace cells that have no roof yet (LayoutWorker_Structure
/// always passes roofs:false to base.Spawn) with the correct buildable roof type.
///
/// Priority order (highest wins):
///   1. LayoutRoomDef.roofDef    — per-room override (built-in RimWorld 1.6 field)
///   2. StructureLayoutRoofExtension.roofDef — layout-wide default on the StructureLayoutDef
///   3. No override              — vanilla AutoBuildRoofAreaSetter handles roofing as normal
///
/// We patch LayoutWorker_Structure.Spawn (not the base LayoutWorker.Spawn) because
/// LayoutWorker_Structure overrides Spawn and calls base.Spawn(roofs:false) internally,
/// so the base postfix always sees roofs=false and returns early.
/// </summary>
[HarmonyPatch]
public static class LayoutWorker_Spawn_Patch
{
  [HarmonyPatch(typeof(LayoutWorker_Structure), nameof(LayoutWorker_Structure.Spawn))]
  [HarmonyPostfix]
  public static void Spawn_Postfix(LayoutStructureSketch layoutStructureSketch, Map map, bool roofs)
  {
    if (!roofs) return;
    if (layoutStructureSketch?.structureLayout == null) return;

    // Layout-wide default from our mod extension
    RoofDef? defaultRoof = (layoutStructureSketch.layoutDef as StructureLayoutDef)?
      .GetModExtension<StructureLayoutRoofExtension>()?.roofDef;
    ThingDef? defaultStuff = (layoutStructureSketch.layoutDef as StructureLayoutDef)?
      .GetModExtension<StructureLayoutRoofExtension>()?.stuffDef;

    if (defaultRoof == null) return; // No custom roof defined — leave vanilla behaviour

    // Apply to named rooms (per-room override takes priority)
    foreach (var room in layoutStructureSketch.structureLayout.Rooms)
    {
      RoofDef? roomRoof = FindRoomRoofDef(room.defs);
      RoofDef effectiveRoof = roomRoof ?? defaultRoof;

      foreach (var rect in room.rects)
        ApplyRoofToRect(rect, map, effectiveRoof);
    }

    // Apply to corridor / container cells not yet covered by any room rect
    foreach (var cell in layoutStructureSketch.structureLayout.container.Cells)
    {
      if (!cell.InBounds(map)) continue;
      if (map.roofGrid.RoofAt(cell) != null) continue; // already set by room loop
      if (!RoofCollapseUtility.WithinRangeOfRoofHolder(cell, map)) continue;
      map.roofGrid.SetRoof(cell, defaultRoof);
      map.GetComponent<RoofIntegrityGrid>()?.InitializeRoof(cell, defaultRoof, defaultStuff);
    }
  }

  private static RoofDef? FindRoomRoofDef(List<LayoutRoomDef> defs)
  {
    if (defs == null) return null;
    foreach (var rd in defs)
      if (rd.roofDef != null) return rd.roofDef;
    return null;
  }

  private static void ApplyRoofToRect(CellRect rect, Map map, RoofDef roofDef)
  {
    foreach (var cell in rect.Cells)
    {
      if (!cell.InBounds(map)) continue;
      if (!RoofCollapseUtility.WithinRangeOfRoofHolder(cell, map)) continue;
      var existing = map.roofGrid.RoofAt(cell);
      if (existing == null || existing == RoofDefOf.RoofConstructed)
        map.roofGrid.SetRoof(cell, roofDef);
    }
  }
}
