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
  public class AtlasEntry
  {
    public Texture2D BaseTexture = null!;
    public List<Vector2[]> FlatVariants = [];
    public Dictionary<(int col, int row), Vector2[]>? SeamlessGrid;
    public int GridWidth;
    public int GridHeight;
    public bool IsSeamless => SeamlessGrid != null;
  }

  public static readonly Dictionary<string, AtlasEntry> uvMap = [];
  private static readonly Dictionary<(Texture2D, Color), (Material cutout, Material transparent)> materialColorCache = [];
  private static readonly Dictionary<Texture2D, Material> metaOverlayCache = [];

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
        CacheUv(gd.texPath, gd.isSeamless, matchingSprites);
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
        CacheUv(atlasDef.texPath, atlasDef.isSeamless, matchingSprites);
      }
      else
      {
        StratumLog.Warning($"Could not find any sprites matching '{fileName}' for texture atlas {atlasDef.defName}.");
      }
    }
  }

  public static AtlasEntry GetOrCreateEntry(string path)
  {
    if (path.NullOrEmpty())
    {
      path = "fallback_empty";
    }

    if (!uvMap.TryGetValue(path, out var entry))
    {
      StratumLog.Warning($"No UV entry found for '{path}', creating fallback entry.");
      var fallbackTex = ContentFinder<Texture2D>.Get(path, false) ?? BaseContent.BadTex ?? Texture2D.whiteTexture;
      entry = new AtlasEntry
      {
        BaseTexture = fallbackTex
      };
      entry.FlatVariants.Add([
        new Vector2(0f, 0f),
        new Vector2(0f, 1f),
        new Vector2(1f, 1f),
        new Vector2(1f, 0f)
      ]);
      uvMap[path] = entry;
    }
    return entry;
  }

  public static AtlasEntry GetEntry(string path)
  {
    return GetOrCreateEntry(path);
  }

  public static (Material cutout, Material transparent) GetMaterials(string path, Color color)
  {
    var entry = GetOrCreateEntry(path);
    return GetMaterials(entry.BaseTexture, color);
  }

  private static (Material cutout, Material transparent) GetMaterials(Texture2D tex, Color color)
  {
    if (tex == null)
    {
      tex = BaseContent.BadTex ?? Texture2D.whiteTexture;
    }
    if (!materialColorCache.TryGetValue((tex, color), out var mats))
    {
      mats = (
        new Material(ShaderDatabase.Cutout) { mainTexture = tex, color = color, name = $"RoofAtlas_{tex.name}_Cutout", renderQueue = 2900 },
        new Material(ShaderDatabase.Transparent) { mainTexture = tex, color = color, name = $"RoofAtlas_{tex.name}_Transparent", renderQueue = 2901 }
      );
      materialColorCache[(tex, color)] = mats;
    }
    return mats;
  }

  public static Material GetMetaOverlay(string path)
  {
    var entry = GetOrCreateEntry(path);
    return GetMetaOverlay(entry.BaseTexture);
  }

  public static Material GetMetaOverlay(Texture2D tex)
  {
    if (tex == null)
    {
      tex = BaseContent.BadTex ?? Texture2D.whiteTexture;
    }
    if (!metaOverlayCache.TryGetValue(tex, out var mat))
    {
      mat = new Material(ShaderDatabase.MetaOverlay) { mainTexture = tex, color = Color.white, name = $"RoofAtlas_{tex.name}_MetaOverlay" };
      mat.renderQueue = 4500;
      metaOverlayCache[tex] = mat;
    }
    return mat;
  }

  private static void CacheUv(string path, bool isSeamless, List<Sprite> spriteList)
  {
    if (uvMap.ContainsKey(path)) return;

    var entry = new AtlasEntry();
    entry.BaseTexture = spriteList[0].texture;

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

        entry.SeamlessGrid[(col, row)] = ExtractQuadUvs(sprite);
      }

      StratumLog.Debug($"Seamless '{Path.GetFileNameWithoutExtension(path)}': {spriteList.Count} sprites, grid {entry.GridWidth}x{entry.GridHeight}");
    }
    else
    {
      foreach (var sprite in spriteList)
      {
        entry.FlatVariants.Add(ExtractQuadUvs(sprite));
      }
    }

    uvMap[path] = entry;
  }

  public static Vector2[]? GetIconUvs(string path)
  {
    if (path.NullOrEmpty()) return null;
    var entry = GetOrCreateEntry(path);
    if (entry.FlatVariants.Count > 0)
    {
      return entry.FlatVariants[0];
    }
    if (entry.SeamlessGrid != null && entry.SeamlessGrid.Count > 0)
    {
      return entry.SeamlessGrid.Values.First();
    }
    return null;
  }

  public static Vector2[]? GetUvs(string path, int variantIndex)
  {
    if (path.NullOrEmpty()) return null;
    var entry = GetOrCreateEntry(path);
    if (entry.FlatVariants.Count > 0)
    {
      return entry.FlatVariants[Mathf.Abs(variantIndex) % entry.FlatVariants.Count];
    }
    return null;
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
