using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

using SolarWeb.Stratum.DefModExtensions;
using SolarWeb.Stratum.Graphics;

namespace SolarWeb.Stratum.Stats;

[StaticConstructorOnStartup]
public static class RoofStatCache
{
  private static readonly Dictionary<int, float> baseBeautyCache = [];
  private static readonly Dictionary<int, float> baseWealthCache = [];
  private static readonly Dictionary<int, float> cleanlinessCache = [];
  private static readonly Dictionary<int, float> solarEfficiencyCache = [];
  private static readonly Dictionary<int, float> transparencyCache = [];
  private static readonly Dictionary<int, float> flammabilityCache = [];
  private static readonly Dictionary<int, int> maxHitPointsCache = [];
  private static readonly Dictionary<int, RoofGraphicData> graphicDataCache = [];
  private static readonly Dictionary<int, Color> colorCache = [];
  private static readonly Dictionary<int, Color> glassTintCache = [];
  private static readonly HashSet<int> buildableCache = [];
  private static readonly HashSet<int> skylightCache = [];

  private static readonly object CacheLock = new();

  static RoofStatCache()
  {
    foreach (var def in DefDatabase<RoofDef>.AllDefs)
    {
      var ext = def.GetModExtension<BuildableRoofExtension>();
      if (ext != null)
      {
        int hash = def.defNameHash;
        buildableCache.Add(hash);

        if (ext.solarEfficiency > 0f) solarEfficiencyCache[hash] = ext.solarEfficiency;
        if (ext.transparency > 0f) transparencyCache[hash] = ext.transparency;
        if (ext.isSkylight) skylightCache.Add(hash);
        if (ext.glassTint.HasValue) glassTintCache[hash] = ext.glassTint.Value;

        if (ext.graphicData != null)
        {
          graphicDataCache[hash] = ext.graphicData;
          colorCache[hash] = ext.graphicData.color;
        }

        var bDef = ext.buildableDef;
        if (bDef != null)
        {
          maxHitPointsCache[hash] = Mathf.RoundToInt(bDef.statBases.GetStatValueFromList(StatDefOf.MaxHitPoints, 100f));
          float flammability = bDef.statBases.GetStatValueFromList(StatDefOf.Flammability, 0f);
          if (flammability > 0f) flammabilityCache[hash] = flammability;

          if (ext.graphicData == null && bDef is ThingDef tDef && tDef.graphicData != null)
          {
            var rg = new RoofGraphicData
            {
              texPath = tDef.graphicData.texPath,
              color = tDef.graphicData.color,
              damageData = tDef.graphicData.damageData
            };
            graphicDataCache[hash] = rg;
            colorCache[hash] = rg.color;
          }

          float beauty = bDef.statBases.GetStatValueFromList(StatDefOf.Beauty, 0f);
          baseBeautyCache[hash] = beauty;

          float cleanliness = bDef.statBases.GetStatValueFromList(StatDefOf.Cleanliness, 0f);
          if (cleanliness != 0f) cleanlinessCache[hash] = cleanliness;

          float wealth = bDef.statBases.GetStatValueFromList(StatDefOf.MarketValue, -1f);
          if (wealth < 0f && !bDef.CostList.NullOrEmpty())
          {
            wealth = 0f;
            foreach (var cost in bDef.CostList)
            {
              wealth += cost.count * cost.thingDef.BaseMarketValue;
            }
          }
          baseWealthCache[hash] = Mathf.Max(0f, wealth);
        }
      }
    }
    RoofAtlasManager.Initialize();
  }

  public static bool IsCustomRoof(RoofDef def) => def != null && buildableCache.Contains(def.defNameHash);
  public static bool IsSkylight(RoofDef def) => def != null && skylightCache.Contains(def.defNameHash);

  private static readonly Dictionary<int, float> roofStuffBeautyCache = [];
  private static readonly Dictionary<int, float> roofStuffWealthCache = [];
  private static readonly Dictionary<int, float> roofStuffFlammabilityCache = [];
  private static readonly Dictionary<int, int> roofStuffMaxHitPointsCache = [];

  public static float GetBeauty(RoofDef def, ThingDef? stuff = null)
  {
    if (stuff == null) return baseBeautyCache.TryGetValue(def.defNameHash, out float b) ? b : 0f;

    int hashKey = def.defNameHash ^ (stuff.defNameHash << 16 | stuff.defNameHash >> 16);
    lock (CacheLock)
    {
      if (roofStuffBeautyCache.TryGetValue(hashKey, out float beauty)) return beauty;

      var ext = def.GetModExtension<BuildableRoofExtension>();
      if (ext?.buildableDef != null)
      {
        beauty = ext.buildableDef.GetStatValueAbstract(StatDefOf.Beauty, stuff);
        roofStuffBeautyCache[hashKey] = beauty;
        return beauty;
      }
    }
    return 0f;
  }

  public static float GetCleanliness(RoofDef def)
  {
    return cleanlinessCache.TryGetValue(def.defNameHash, out float val) ? val : 0f;
  }

  public static float GetWealth(RoofDef def, ThingDef? stuff = null)
  {
    if (stuff == null) return baseWealthCache.TryGetValue(def.defNameHash, out float w) ? w : 0f;

    int hashKey = def.defNameHash ^ (stuff.defNameHash << 16 | stuff.defNameHash >> 16);
    lock (CacheLock)
    {
      if (roofStuffWealthCache.TryGetValue(hashKey, out float wealth)) return wealth;

      var ext = def.GetModExtension<BuildableRoofExtension>();
      if (ext?.buildableDef != null)
      {
        wealth = ext.buildableDef.GetStatValueAbstract(StatDefOf.MarketValue, stuff);
        roofStuffWealthCache[hashKey] = wealth;
        return wealth;
      }
    }
    return 0f;
  }

  public static float GetSolarEfficiency(RoofDef def)
  {
    return solarEfficiencyCache.TryGetValue(def.defNameHash, out float val) ? val : 0f;
  }

  public static float GetTransparency(RoofDef def)
  {
    return transparencyCache.TryGetValue(def.defNameHash, out float val) ? val : 0f;
  }

  public static float GetFlammability(RoofDef def, ThingDef? stuff = null)
  {
    if (stuff == null) return flammabilityCache.TryGetValue(def.defNameHash, out float f) ? f : 0f;

    int hashKey = def.defNameHash ^ (stuff.defNameHash << 16 | stuff.defNameHash >> 16);
    lock (CacheLock)
    {
      if (roofStuffFlammabilityCache.TryGetValue(hashKey, out float flammability)) return flammability;

      var ext = def.GetModExtension<BuildableRoofExtension>();
      if (ext?.buildableDef != null)
      {
        flammability = ext.buildableDef.GetStatValueAbstract(StatDefOf.Flammability, stuff);
        roofStuffFlammabilityCache[hashKey] = flammability;
        return flammability;
      }
    }
    return 0f;
  }

  public static int GetMaxHitPoints(RoofDef def, ThingDef? stuff = null)
  {
    if (stuff == null) return maxHitPointsCache.TryGetValue(def.defNameHash, out int val) ? val : 0;

    int hashKey = def.defNameHash ^ (stuff.defNameHash << 16 | stuff.defNameHash >> 16);
    lock (CacheLock)
    {
      if (roofStuffMaxHitPointsCache.TryGetValue(hashKey, out int hp)) return hp;

      var ext = def.GetModExtension<BuildableRoofExtension>();
      if (ext?.buildableDef != null)
      {
        hp = Mathf.RoundToInt(ext.buildableDef.GetStatValueAbstract(StatDefOf.MaxHitPoints, stuff));
        roofStuffMaxHitPointsCache[hashKey] = hp;
        return hp;
      }
    }
    return 0;
  }

  public static RoofGraphicData? GetGraphicData(RoofDef def)
  {
    return graphicDataCache.TryGetValue(def.defNameHash, out var val) ? val : null;
  }

  public static Color GetColor(RoofDef def, ThingDef? stuff = null)
  {
    if (stuff != null && stuff.stuffProps != null) return stuff.stuffProps.color;
    return colorCache.TryGetValue(def.defNameHash, out var val) ? val : Color.white;
  }

  public static Color GetGlassTint(RoofDef def, Map? map = null, IntVec3 cell = default)
  {
    if (map != null && cell.IsValid)
    {
      var tint = map.GetComponent<MapComponents.RoofIntegrityGrid>()?.GetGlassTint(cell);
      if (tint.HasValue) return tint.Value;
    }
    return glassTintCache.TryGetValue(def.defNameHash, out var val) ? val : Color.white;
  }

  public static RoofEdgeGraphicData? GetEdgeGraphicData(RoofDef def)
  {
    return GetGraphicData(def)?.edgeData;
  }

  public static RoofEdgeGraphicData? GetSkylightEdgeGraphicData(RoofDef def)
  {
    return GetGraphicData(def)?.skylightEdgeData;
  }
}
