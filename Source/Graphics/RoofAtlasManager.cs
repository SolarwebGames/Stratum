using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using System.IO;

using SolarWeb.Stratum.Stats;

namespace SolarWeb.Stratum.Graphics;

[StaticConstructorOnStartup]
public static class RoofAtlasManager
{
  internal record struct UvEntry(Vector2[] Uvs, Material Mat);

  internal class AtlasEntry
  {
    public List<UvEntry> FlatVariants = [];
    public Dictionary<(int col, int row), UvEntry>? SeamlessGrid;
    public int GridWidth;
    public int GridHeight;
  }

  internal static readonly Dictionary<string, AtlasEntry> uvMap = [];
  private static readonly Dictionary<Texture2D, (Material cutout, Material transparent)> materialCache = [];

  public static void Initialize()
  {
    var bundle = AssetBundleManager.AssetBundle;
    if (bundle == null)
    {
      StratumLog.Error("AssetBundle is null; cannot initialize RoofAtlasManager.");
      return;
    }

    var allSprites = bundle.LoadAllAssets<Sprite>();
    StratumLog.Debug($"Found {allSprites.Length} Sprites in bundle.");

    foreach (var def in DefDatabase<RoofDef>.AllDefs)
    {
      var gd = RoofStatCache.GetGraphicData(def);
      if (gd == null || gd.texPath.NullOrEmpty()) continue;

      string fileName = Path.GetFileNameWithoutExtension(gd.texPath);

      // Collect all sprites that match this def's name exactly or as a prefix (e.g., MyRoof or MyRoof_0)
      var matchingSprites = allSprites.Where(s =>
      {
        string name = s.name;
        if (name.EndsWith("(Clone)")) name = name[..^7].Trim();
        return name == fileName || name.StartsWith(fileName + "_");
      }).ToList();

      if (matchingSprites.Count > 0)
      {
        bool isSkylight = RoofStatCache.IsSkylight(def);
        CacheUv(gd.texPath, gd.isSeamless, isSkylight, matchingSprites);
      }
      else
      {
        StratumLog.Warning($"Could not find any sprites matching '{fileName}' for {def.defName}.");
      }
    }

    foreach (var atlasDef in DefDatabase<Defs.StratumTextureAtlasDef>.AllDefs)
    {
      if (atlasDef.texPath.NullOrEmpty()) continue;

      string fileName = Path.GetFileNameWithoutExtension(atlasDef.texPath);

      var matchingSprites = allSprites.Where(s =>
      {
        string name = s.name;
        if (name.EndsWith("(Clone)")) name = name[..^7].Trim();
        return name == fileName || name.StartsWith(fileName + "_");
      }).ToList();

      if (matchingSprites.Count > 0)
      {
        CacheUv(atlasDef.texPath, atlasDef.isSeamless, atlasDef.isTransparent, matchingSprites);
      }
      else
      {
        StratumLog.Warning($"Could not find any sprites matching '{fileName}' for texture atlas {atlasDef.defName}.");
      }
    }
  }

  private static (Material cutout, Material transparent) GetMaterials(Texture2D tex)
  {
    if (!materialCache.TryGetValue(tex, out var mats))
    {
      var cutout = new Material(ShaderDatabase.MetaOverlay) { mainTexture = tex, color = Color.white, name = $"RoofAtlas_{tex.name}_Cutout" };
      cutout.renderQueue = 4500;

      var trans = new Material(ShaderDatabase.Transparent) { mainTexture = tex, color = Color.white, name = $"RoofAtlas_{tex.name}_Transparent" };
      trans.renderQueue = 4500;

      mats = (cutout, trans);
      materialCache[tex] = mats;
    }
    return mats;
  }

  private static void CacheUv(string path, bool isSeamless, bool isTransparent, List<Sprite> spriteList)
  {
    if (uvMap.ContainsKey(path)) return;

    var entry = new AtlasEntry();

    if (isSeamless)
    {
      float minX = spriteList.Min(s => s.rect.x);
      float minY = spriteList.Min(s => s.rect.y);
      float maxX = spriteList.Max(s => s.rect.xMax);
      float maxY = spriteList.Max(s => s.rect.yMax);

      // We assume all sprites in the atlas have the same dimensions
      float firstWidth = spriteList[0].rect.width;
      float firstHeight = spriteList[0].rect.height;

      entry.GridWidth = Mathf.RoundToInt((maxX - minX) / firstWidth);
      entry.GridHeight = Mathf.RoundToInt((maxY - minY) / firstHeight);
      entry.SeamlessGrid = [];

      foreach (var sprite in spriteList)
      {
        // Calculate col/row based on the sprite's pixel coordinates.
        // row=0 will be the bottom-most slices, matching RimWorld worldZ=0.
        int col = Mathf.RoundToInt((sprite.rect.x - minX) / firstWidth);
        int row = Mathf.RoundToInt((sprite.rect.y - minY) / firstHeight);

        var mats = GetMaterials(sprite.texture);
        var mat = isTransparent ? mats.transparent : mats.cutout;
        entry.SeamlessGrid[(col, row)] = new UvEntry(ExtractQuadUvs(sprite), mat);
      }

      StratumLog.Debug($"Seamless '{Path.GetFileNameWithoutExtension(path)}': {spriteList.Count} sprites, grid {entry.GridWidth}x{entry.GridHeight}");
    }
    else
    {
      foreach (var sprite in spriteList)
      {
        var mats = GetMaterials(sprite.texture);
        var mat = isTransparent ? mats.transparent : mats.cutout;
        entry.FlatVariants.Add(new UvEntry(ExtractQuadUvs(sprite), mat));
      }
    }

    uvMap[path] = entry;
  }

  public static bool TryGetUv(string path, out Vector2[]? uvs, out Material? mat)
    => TryGetUv(path, 0, out uvs, out mat);

  public static bool TryGetUv(string path, int variantIndex, out Vector2[]? uvs, out Material? mat)
  {
    if (uvMap.TryGetValue(path, out var entry))
    {
      if (entry.FlatVariants.Count > 0)
      {
        var e = entry.FlatVariants[Mathf.Abs(variantIndex) % entry.FlatVariants.Count];
        uvs = e.Uvs;
        mat = e.Mat;
        return true;
      }

      if (entry.SeamlessGrid != null && entry.SeamlessGrid.Count > 0)
      {
        // For seamless atlases, we pick the first available tile as a representative icon
        var e = entry.SeamlessGrid.Values.First();
        uvs = e.Uvs;
        mat = e.Mat;
        return true;
      }
    }
    uvs = null;
    mat = null;
    return false;
  }

  public static bool TryGetSeamlessUv(string path, int worldX, int worldZ, out Vector2[]? uvs, out Material? mat)
  {
    if (uvMap.TryGetValue(path, out var entry) && entry.SeamlessGrid != null)
    {
      int col = ((worldX % entry.GridWidth) + entry.GridWidth) % entry.GridWidth;
      int row = ((worldZ % entry.GridHeight) + entry.GridHeight) % entry.GridHeight;

      if (entry.SeamlessGrid.TryGetValue((col, row), out var e))
      {
        uvs = e.Uvs;
        mat = e.Mat;
        return true;
      }
    }
    uvs = null;
    mat = null;
    return false;
  }

  private static Vector2[] ExtractQuadUvs(Sprite sprite)
  {
    var rect = sprite.rect;
    var tex = sprite.texture;
    float tw = tex.width;
    float th = tex.height;

    float minU = rect.x / tw;
    float maxU = rect.xMax / tw;
    float minV = rect.y / th;
    float maxV = rect.yMax / th;

    return [
      new Vector2(minU, minV), // BL
      new Vector2(minU, maxV), // TL
      new Vector2(maxU, maxV), // TR
      new Vector2(maxU, minV)  // BR
    ];
  }
}
