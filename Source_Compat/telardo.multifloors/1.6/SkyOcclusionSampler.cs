using UnityEngine;
using Verse;

using SolarWeb.Stratum.MultiFloors.MapComponents;
using SolarWeb.Stratum.Stats;
using global::MultiFloors;

namespace SolarWeb.Stratum.MultiFloors;

/// <summary>
/// How much sunlight the MultiFloors levels above a cell let through, and what colour it becomes.
/// </summary>
internal readonly struct SkyOcclusion(float transmission, Color tint)
{
  public readonly float Transmission = transmission;
  public readonly Color Tint = tint;

  public static readonly SkyOcclusion Clear = new(1f, Color.white);
}

/// <summary>
/// Walks the MultiFloors level stack above a cell, accumulating how much light reaches it and what
/// the glass along the way does to the colour.
/// </summary>
/// <remarks>
/// Stacking is multiplicative in both channels: two 60% panes transmit 36%, and their tints
/// multiply componentwise, so blue glass under amber glass reads as the muddy green-brown real
/// glass would produce.
/// </remarks>
internal static class SkyOcclusionSampler
{
  private static bool[]? transparentByTerrainIndex;

  public static SkyOcclusion Sample(Map map, IntVec3 cell)
  {
    if (map == null || !cell.InBounds(map)) return SkyOcclusion.Clear;

    // Fast path for the common single-level case: UpperMap is a Prepatcher field accessor, so a
    // colony with nothing stacked above pays one field read per call.
    Map upper = map.UpperMap();
    if (upper == null) return SkyOcclusion.Clear;

    var cache = SkyOcclusionCache.For(map);
    if (cache != null && cache.TryGet(cell, out float cachedTransmission, out Color cachedTint))
    {
      return new SkyOcclusion(cachedTransmission, cachedTint);
    }

    float transmission = 1f;
    Color tint = Color.white;

    for (Map cursor = upper; cursor != null; cursor = cursor.UpperMap())
    {
      if (!cell.InBounds(cursor)) continue;

      // Roof and terrain are checked independently, not else-if: a level can carry both a glass
      // roof and an opaque floor, and the floor still stops the light.
      var roof = cursor.roofGrid?.RoofAt(cell);
      if (roof != null)
      {
        if (!RoofStatCache.IsSkylight(roof))
        {
          transmission = 0f;
          break;
        }

        transmission *= RoofStatCache.GetEffectiveTransparency(roof, cursor, cell);
        tint *= RoofStatCache.GetGlassTint(roof, cursor, cell);

        if (transmission <= 0.0001f)
        {
          transmission = 0f;
          break;
        }
      }

      var terrain = cursor.terrainGrid?.TerrainAt(cell);
      if (terrain != null && !IsTransparent(terrain))
      {
        transmission = 0f;
        break;
      }
    }

    // Colour is meaningless once nothing gets through.
    if (transmission <= 0f) tint = Color.white;
    else tint.a = 1f;

    cache?.Store(cell, transmission, tint);
    return new SkyOcclusion(transmission, tint);
  }

  /// <summary>
  /// MultiFloors' own check is a <c>List&lt;TerrainDef&gt;.Contains</c> linear scan; defs are fixed
  /// after load, so bake it into a lookup indexed by terrain index on first use.
  /// </summary>
  private static bool IsTransparent(TerrainDef terrain)
  {
    if (terrain == null) return true;

    var table = transparentByTerrainIndex;
    if (table == null)
    {
      var settings = MiscDefOfs.MF_UpperLevelSettings;
      if (settings == null) return true;

      int size = DefDatabase<TerrainDef>.DefCount;
      table = new bool[size];
      foreach (var def in DefDatabase<TerrainDef>.AllDefs)
      {
        if (def.index >= 0 && def.index < size)
        {
          table[def.index] = settings.IsTransparentTerrain(def);
        }
      }
      transparentByTerrainIndex = table;
    }

    int idx = terrain.index;
    return idx >= 0 && idx < table.Length && table[idx];
  }
}
