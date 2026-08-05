using System.Collections.Generic;
using UnityEngine;
using Verse;

using global::MultiFloors;

namespace SolarWeb.Stratum.MultiFloors.MapComponents;

/// <summary>
/// Per-cell cache of how much light the MultiFloors levels above a map let through, and what
/// colour it arrives as.
/// </summary>
/// <remarks>
/// This is not merely an optimisation. <c>GlowGrid.GroundGlowAt</c> consults the occlusion hooks
/// constantly -- plant growth, <c>PsychGlowAt</c>, darkness moods -- and without a cache every one
/// of those calls would walk the entire level stack doing roof and terrain lookups per level.
///
/// Storage is allocated lazily and only for maps that actually have a level above them, so a
/// single-level colony pays nothing. Entries are dropped wholesale when <see cref="Generation"/>
/// changes, which happens whenever a map is added to or removed from the world.
/// </remarks>
public class SkyOcclusionCache(Map map) : MapComponent(map)
{
  /// <summary>
  /// Bumped whenever the set of maps changes, invalidating every cache. MultiFloors exposes no
  /// level-added/removed event, so this rides on the generic map lifecycle instead.
  /// </summary>
  public static int Generation;

  private static readonly Dictionary<Map, SkyOcclusionCache> lookup = [];

  private float[]? transmission;
  private Color32[]? tint;
  private bool[]? valid;
  private int builtAtGeneration = -1;

  public static SkyOcclusionCache? For(Map map)
  {
    if (map == null) return null;
    if (lookup.TryGetValue(map, out var cached)) return cached;

    var component = map.GetComponent<SkyOcclusionCache>();
    if (component != null) lookup[map] = component;
    return component;
  }

  public override void MapRemoved()
  {
    base.MapRemoved();
    lookup.Remove(map);
    Generation++;
  }

  public override void FinalizeInit()
  {
    base.FinalizeInit();
    lookup[map] = this;
    // A map finishing init may have just been slotted into a level stack, changing what is above
    // every other map on the tile.
    Generation++;
  }

  private bool EnsureBuffers()
  {
    if (builtAtGeneration != Generation)
    {
      builtAtGeneration = Generation;
      if (valid != null) System.Array.Clear(valid, 0, valid.Length);
    }

    if (transmission != null) return true;

    if (map == null) return false;
    int count = map.cellIndices.NumGridCells;
    if (count <= 0) return false;

    transmission = new float[count];
    tint = new Color32[count];
    valid = new bool[count];
    return true;
  }

  public bool TryGet(IntVec3 cell, out float outTransmission, out Color outTint)
  {
    outTransmission = 1f;
    outTint = Color.white;

    if (builtAtGeneration != Generation || transmission == null || valid == null || tint == null) return false;
    if (!cell.InBounds(map)) return false;

    int idx = map.cellIndices.CellToIndex(cell);
    if (!valid[idx]) return false;

    outTransmission = transmission[idx];
    outTint = tint[idx];
    return true;
  }

  public void Store(IntVec3 cell, float value, Color colour)
  {
    if (!EnsureBuffers()) return;
    if (!cell.InBounds(map)) return;

    int idx = map.cellIndices.CellToIndex(cell);
    transmission![idx] = value;
    tint![idx] = colour;
    valid![idx] = true;
  }

  public void Invalidate(IntVec3 cell)
  {
    if (valid == null || !cell.InBounds(map)) return;
    valid[map.cellIndices.CellToIndex(cell)] = false;
  }

  public void InvalidateAll()
  {
    if (valid != null) System.Array.Clear(valid, 0, valid.Length);
  }
}
