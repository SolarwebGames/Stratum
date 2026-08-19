using System.Collections.Generic;
using UnityEngine;
using Verse;

using SolarWeb.Stratum.MapComponents;
using SolarWeb.Stratum.Stats;

namespace SolarWeb.Stratum.Graphics;

/// <summary>
/// Options controlling how <see cref="RoofCellPainter"/> paints a roof cell.
/// </summary>
/// <remarks>
/// <c>default</c> must reproduce <see cref="CustomRoofsRenderer"/>'s behaviour exactly, so every
/// field is phrased such that its zero value is the current-map default.
/// </remarks>
public struct RoofPaintOptions
{
  /// <summary>
  /// Base render queue for painted materials, or <c>null</c> to keep Stratum's own queues
  /// (2900 cutout / 2901 transparent / 4500 meta overlay). Glass is drawn at base + 1.
  /// </summary>
  public int? RenderQueueOverride;

  /// <summary>When <c>true</c>, skip the damage scratch overlay.</summary>
  public bool SuppressDamageScratches;

  /// <summary>
  /// When <c>true</c>, natural roofs draw as a single flat quad instead of the 3x3 grid whose
  /// outer ring fades into neighbouring rock.
  /// </summary>
  /// <remarks>
  /// That fade is built from a per-vertex alpha ramp on a <c>Map/MetaOverlay</c> material, which
  /// only composites correctly at its native render queue (4500) -- late, after the scene is drawn.
  /// Anywhere the roof has to be re-queued to sit among ordinary geometry, the ramp blends against
  /// a half-drawn framebuffer and produces a shifting bright band along the cell edges.
  ///
  /// Flattening trades a subtle edge blend for order-independent rendering. The blend is there to
  /// soften rock-to-rock transitions when you are standing on that level; a floor down it is not
  /// really perceptible anyway.
  /// </remarks>
  public bool FlattenNaturalRoofEdges;
}

/// <summary>
/// Paints a single Stratum roof cell into a <see cref="MapDrawLayer"/>.
/// </summary>
/// <remarks>
/// Extracted from <see cref="CustomRoofsRenderer"/> so the same rendering can be driven for a map
/// other than the one owning the layer -- specifically, so the MultiFloors compatibility module can
/// paint a lower level's roofs into the upper level's section mesh. Every method here takes the
/// target layer and the source map explicitly rather than relying on <c>this</c> and
/// <c>MapDrawLayer.Map</c>.
///
/// <see cref="MapDrawLayer.GetSubMesh(Material)"/> is public, so painting into a foreign layer is
/// legitimate -- but it must happen between that layer's <c>ClearSubMeshes</c> and
/// <c>FinalizeMesh</c>, i.e. from inside its own <c>Regenerate</c>.
/// </remarks>
public static class RoofCellPainter
{
  private static Material[]? defaultScratchMats;
  private static Material[] DefaultScratchMats =>
    defaultScratchMats ??= [
      MaterialPool.MatFrom(RimWorldTextures.Damage.Scratch1, ShaderDatabase.Cutout),
      MaterialPool.MatFrom(RimWorldTextures.Damage.Scratch2, ShaderDatabase.Cutout),
      MaterialPool.MatFrom(RimWorldTextures.Damage.Scratch3, ShaderDatabase.Cutout)
    ];

  private static Material? fallbackMat;
  public static Material FallbackMat =>
    fallbackMat ??= MaterialPool.MatFrom(RimWorldTextures.Terrain.Surfaces.Concrete, ShaderDatabase.Cutout, new Color(0.5f, 0.5f, 0.5f));

  private static readonly Dictionary<(Material, int), Material> requeuedMaterials = [];

  /// <summary>
  /// Returns <paramref name="src"/> re-queued to <paramref name="queue"/>, or unchanged when no
  /// override is requested. Clones are cached so material identity stays stable and
  /// <see cref="MapDrawLayer.GetSubMesh(Material)"/> keeps deduplicating correctly.
  /// </summary>
  public static Material AtQueue(Material src, int? queue)
  {
    // Pass-through: null in means null out, and every caller sources its material from
    // RoofAtlasManager, which never returns null.
    if (src == null || queue == null || src.renderQueue == queue.Value) return src!;

    var key = (src, queue.Value);
    if (!requeuedMaterials.TryGetValue(key, out var requeued))
    {
      requeued = new Material(src) { renderQueue = queue.Value, name = src.name + "_rq" + queue.Value };
      requeuedMaterials[key] = requeued;
    }
    return requeued;
  }

  private static int? Offset(int? queue, int by) => queue.HasValue ? queue.Value + by : null;

  /// <summary>
  /// Picks the atlas UVs for a cell: the seamless grid slot when the entry tiles, otherwise a
  /// hash-stable flat variant. Returns <c>null</c> when the entry has nothing usable, in which
  /// case the caller should draw nothing.
  /// </summary>
  public static Vector2[]? ResolveUvs(RoofAtlasManager.AtlasEntry entry, IntVec3 c)
  {
    if (entry == null) return null;

    if (entry.IsSeamless && entry.SeamlessGrid != null)
    {
      int col = c.x % entry.GridWidth;
      if (col < 0) col += entry.GridWidth;

      int row = c.z % entry.GridHeight;
      if (row < 0) row += entry.GridHeight;

      return entry.SeamlessGrid.TryGetValue((col, row), out var uvs) ? uvs : null;
    }

    return entry.FlatVariants.Count > 0
      ? entry.FlatVariants[Mathf.Abs(c.GetHashCode()) % entry.FlatVariants.Count]
      : null;
  }

  /// <summary>
  /// Paints one roof cell: texture, colour, framed skylight or natural-roof edge blending as
  /// appropriate, plus damage scratches.
  /// </summary>
  public static void PrintRoofCell(MapDrawLayer layer, Map map, IntVec3 c, RoofDef roof,
                                   RoofIntegrityGrid? integrityGrid, float altitude,
                                   in RoofPaintOptions opts = default)
  {
    if (layer == null || map == null || roof == null) return;

    RoofGraphicData? graphicData = null;
    float alpha = 1f;

    if (RoofStatCache.IsCustomRoof(roof))
    {
      graphicData = RoofStatCache.GetGraphicData(roof);
      ThingDef? stuff = integrityGrid?.GetStuff(c);
      Color roofColor = RoofStatCache.GetColor(roof, stuff);

      bool skylight = RoofStatCache.IsSkylight(roof);
      if (skylight) alpha = 1f - RoofStatCache.GetTransparency(roof);
      roofColor.a *= alpha;

      Vector3 center = new(c.x + 0.5f, altitude, c.z + 0.5f);

      if (graphicData != null)
      {
        var entry = RoofAtlasManager.GetEntry(graphicData.texPath);
        var uvs = ResolveUvs(entry, c);

        // A seamless entry with no slot for this cell draws nothing, matching the original.
        if (uvs != null)
        {
          var (cutout, transparent) = RoofAtlasManager.GetMaterials(graphicData.texPath, roofColor);

          if (roof.isNatural && !opts.FlattenNaturalRoofEdges)
          {
            // MetaOverlay is the only shader that honours both the per-vertex alpha ramp and the
            // vertex colours PrintNaturalRoof relies on -- but only at its native queue 4500,
            // where it composites after the scene. Callers that must re-queue it should set
            // FlattenNaturalRoofEdges instead of swapping the shader.
            PrintNaturalRoof(layer, map, c, RoofAtlasManager.GetMetaOverlay(graphicData.texPath),
                             roofColor, altitude, integrityGrid, uvs);
          }
          else if (roof.isNatural)
          {
            // Flat variant: colour baked into the material, no alpha ramp, so nothing depends on
            // draw order.
            PrintQuad(layer, center, Vector2.one,
                      AtQueue(RoofAtlasManager.GetMaterials(graphicData.texPath, roofColor).cutout, opts.RenderQueueOverride),
                      Color.white, 0f, uvs);
          }
          else if (skylight && graphicData.skylightFrameWidth > 0f)
          {
            PrintFramedSkylight(layer, map, c, roof, graphicData, altitude, stuff, uvs, graphicData.texPath, opts);
          }
          else
          {
            Material mat = skylight
              ? AtQueue(transparent, Offset(opts.RenderQueueOverride, 1))
              : AtQueue(cutout, opts.RenderQueueOverride);

            PrintQuad(layer, center, Vector2.one, mat, Color.white, 0f, uvs);
          }
        }
      }
      else
      {
        PrintQuad(layer, center, Vector2.one, AtQueue(FallbackMat, opts.RenderQueueOverride), roofColor, 0f);
      }
    }

    if (!opts.SuppressDamageScratches && integrityGrid != null && Stratum.Settings.enableRoofDamageScratches)
    {
      short hp = integrityGrid.GetHitPoints(c);
      short maxHp = integrityGrid.GetMaxHitPoints(c);

      if (hp > 0 && hp < maxHp)
      {
        // Scratches sit slightly above the roof surface.
        PrintDamageScratches(layer, c, graphicData, altitude + 0.05f, hp, maxHp, alpha, opts);
      }
    }
  }

  public static void PrintFramedSkylight(MapDrawLayer layer, Map map, IntVec3 c, RoofDef roof, RoofGraphicData gd,
                                         float y, ThingDef? stuff, Vector2[] uv, string texPath,
                                         in RoofPaintOptions opts = default)
  {
    float f = gd.skylightFrameWidth;
    float glassAlpha = 1f - RoofStatCache.GetTransparency(roof);

    Color frameColor = RoofStatCache.GetColor(roof, stuff);
    frameColor.a = 1f;

    Color glassColor = RoofStatCache.GetGlassTint(roof, map, c);
    glassColor.a = glassAlpha;

    Material frameMat = AtQueue(RoofAtlasManager.GetMaterials(texPath, frameColor).cutout, opts.RenderQueueOverride);
    Material glassMat = AtQueue(RoofAtlasManager.GetMaterials(texPath, glassColor).transparent, Offset(opts.RenderQueueOverride, 1));

    Vector3 basePos = new(c.x, y, c.z);

    // 9-slice positions (0.0 to 1.0)
    System.ReadOnlySpan<float> p = stackalloc float[] { 0f, f, 1f - f, 1f };

    Vector2 bl = uv[0], tl = uv[1], tr = uv[2], br = uv[3];

    for (int x = 0; x < 3; x++)
    {
      for (int z = 0; z < 3; z++)
      {
        bool isCenter = (x == 1 && z == 1);
        Material quadMat = isCenter ? glassMat : frameMat;

        Vector3 qCenter = basePos + new Vector3((p[x] + p[x + 1]) / 2f, 0, (p[z] + p[z + 1]) / 2f);
        Vector2 qSize = new(p[x + 1] - p[x], p[z + 1] - p[z]);

        System.Span<Vector2> qUv = stackalloc Vector2[4];

        Vector2 GetUv(float px, float pz)
        {
          Vector2 bottom = Vector2.Lerp(bl, br, px);
          Vector2 top = Vector2.Lerp(tl, tr, px);
          return Vector2.Lerp(bottom, top, pz);
        }

        qUv[0] = GetUv(p[x], p[z]);         // BL
        qUv[1] = GetUv(p[x], p[z + 1]);     // TL
        qUv[2] = GetUv(p[x + 1], p[z + 1]); // TR
        qUv[3] = GetUv(p[x + 1], p[z]);     // BR

        // Vertex color is white because color is baked into the material
        PrintQuad(layer, qCenter, qSize, quadMat, Color.white, 0f, qUv);
      }
    }
  }

  public static void PrintNaturalRoof(MapDrawLayer layer, Map map, IntVec3 c, Material mat, Color roofColor,
                                      float altitude, RoofIntegrityGrid? integrityGrid, Vector2[] uv)
  {
    bool hasW = HasRoofAt(map, c.x - 1, c.z);
    bool hasE = HasRoofAt(map, c.x + 1, c.z);
    bool hasS = HasRoofAt(map, c.x, c.z - 1);
    bool hasN = HasRoofAt(map, c.x, c.z + 1);
    bool hasSW = HasRoofAt(map, c.x - 1, c.z - 1);
    bool hasNW = HasRoofAt(map, c.x - 1, c.z + 1);
    bool hasSE = HasRoofAt(map, c.x + 1, c.z - 1);
    bool hasNE = HasRoofAt(map, c.x + 1, c.z + 1);

    System.Span<Color> colors = stackalloc Color[16];
    colors.Fill(roofColor);

    colors[0 * 4 + 1] = GetEdgeColor(map, integrityGrid, roofColor, new IntVec3(c.x - 1, 0, c.z), !hasW);
    colors[0 * 4 + 2] = colors[0 * 4 + 1];

    colors[3 * 4 + 1] = GetEdgeColor(map, integrityGrid, roofColor, new IntVec3(c.x + 1, 0, c.z), !hasE);
    colors[3 * 4 + 2] = colors[3 * 4 + 1];

    colors[1 * 4 + 0] = GetEdgeColor(map, integrityGrid, roofColor, new IntVec3(c.x, 0, c.z - 1), !hasS);
    colors[2 * 4 + 0] = colors[1 * 4 + 0];

    colors[1 * 4 + 3] = GetEdgeColor(map, integrityGrid, roofColor, new IntVec3(c.x, 0, c.z + 1), !hasN);
    colors[2 * 4 + 3] = colors[1 * 4 + 3];

    colors[0 * 4 + 0] = GetCornerColor(map, integrityGrid, roofColor, new IntVec3(c.x - 1, 0, c.z), new IntVec3(c.x, 0, c.z - 1), new IntVec3(c.x - 1, 0, c.z - 1), !(hasW && hasS && hasSW));
    colors[0 * 4 + 3] = GetCornerColor(map, integrityGrid, roofColor, new IntVec3(c.x - 1, 0, c.z), new IntVec3(c.x, 0, c.z + 1), new IntVec3(c.x - 1, 0, c.z + 1), !(hasW && hasN && hasNW));
    colors[3 * 4 + 0] = GetCornerColor(map, integrityGrid, roofColor, new IntVec3(c.x + 1, 0, c.z), new IntVec3(c.x, 0, c.z - 1), new IntVec3(c.x + 1, 0, c.z - 1), !(hasE && hasS && hasSE));
    colors[3 * 4 + 3] = GetCornerColor(map, integrityGrid, roofColor, new IntVec3(c.x + 1, 0, c.z), new IntVec3(c.x, 0, c.z + 1), new IntVec3(c.x + 1, 0, c.z + 1), !(hasE && hasN && hasNE));

    float f = 0.25f;
    System.ReadOnlySpan<float> p = stackalloc float[] { 0f, f, 1f - f, 1f };

    Vector3 basePos = new(c.x, altitude, c.z);
    Vector2 bl = uv[0], tl = uv[1], tr = uv[2], br = uv[3];

    Vector2 GetUv(float px, float pz)
    {
      Vector2 bottom = Vector2.Lerp(bl, br, px);
      Vector2 top = Vector2.Lerp(tl, tr, px);
      return Vector2.Lerp(bottom, top, pz);
    }

    System.Span<Vector2> qUv = stackalloc Vector2[4];
    System.Span<Color> qColors = stackalloc Color[4];

    for (int x = 0; x < 3; x++)
    {
      for (int z = 0; z < 3; z++)
      {
        Vector3 qCenter = basePos + new Vector3((p[x] + p[x + 1]) / 2f, 0, (p[z] + p[z + 1]) / 2f);
        Vector2 qSize = new(p[x + 1] - p[x], p[z + 1] - p[z]);

        qUv[0] = GetUv(p[x], p[z]);
        qUv[1] = GetUv(p[x], p[z + 1]);
        qUv[2] = GetUv(p[x + 1], p[z + 1]);
        qUv[3] = GetUv(p[x + 1], p[z]);

        qColors[0] = colors[x * 4 + z];
        qColors[1] = colors[x * 4 + (z + 1)];
        qColors[2] = colors[(x + 1) * 4 + (z + 1)];
        qColors[3] = colors[(x + 1) * 4 + z];

        PrintQuad(layer, qCenter, qSize, mat, roofColor, 0f, qUv, qColors);
      }
    }
  }

  private static bool HasRoofAt(Map map, int x, int z)
  {
    IntVec3 cell = new(x, 0, z);
    return cell.InBounds(map) && map.roofGrid.RoofAt(cell) != null;
  }

  private static Color GetEdgeColor(Map map, RoofIntegrityGrid? integrityGrid, Color selfColor, IntVec3 neighbor, bool fades)
  {
    float r = selfColor.r;
    float g = selfColor.g;
    float b = selfColor.b;
    float a = selfColor.a;
    int count = 1;

    if (neighbor.InBounds(map))
    {
      var rf = map.roofGrid.RoofAt(neighbor);
      if (rf != null && rf.isNatural && RoofStatCache.IsCustomRoof(rf))
      {
        var stuff = integrityGrid?.GetStuff(neighbor);
        Color c = RoofStatCache.GetColor(rf, stuff);
        r += c.r;
        g += c.g;
        b += c.b;
        a += c.a;
        count++;
      }
    }

    Color avgColor = new(r / count, g / count, b / count, a / count);
    if (fades)
    {
      avgColor.a = 0f;
    }
    return avgColor;
  }

  private static Color GetCornerColor(Map map, RoofIntegrityGrid? integrityGrid, Color selfColor, IntVec3 n1, IntVec3 n2, IntVec3 n3, bool fades)
  {
    float r = selfColor.r;
    float g = selfColor.g;
    float b = selfColor.b;
    float a = selfColor.a;
    int count = 1;

    void AddIfNatural(IntVec3 cell)
    {
      if (cell.InBounds(map))
      {
        var rf = map.roofGrid.RoofAt(cell);
        if (rf != null && rf.isNatural && RoofStatCache.IsCustomRoof(rf))
        {
          var stuff = integrityGrid?.GetStuff(cell);
          Color c = RoofStatCache.GetColor(rf, stuff);
          r += c.r;
          g += c.g;
          b += c.b;
          a += c.a;
          count++;
        }
      }
    }

    AddIfNatural(n1);
    AddIfNatural(n2);
    AddIfNatural(n3);

    Color avgColor = new(r / count, g / count, b / count, a / count);
    if (fades)
    {
      avgColor.a = 0f;
    }
    return avgColor;
  }

  /// <summary>Appends a single quad to <paramref name="layer"/>'s submesh for <paramref name="mat"/>.</summary>
  public static void PrintQuad(MapDrawLayer layer, Vector3 center, Vector2 size, Material mat, Color color,
                               float angle = 0f, System.ReadOnlySpan<Vector2> uvArray = default,
                               System.ReadOnlySpan<Color> vertexColors = default)
  {
    LayerSubMesh subMesh = layer.GetSubMesh(mat);
    if (subMesh == null) return;

    int vCount = subMesh.verts.Count;

    Vector3 v1 = new(-size.x / 2f, 0, -size.y / 2f);
    Vector3 v2 = new(-size.x / 2f, 0, size.y / 2f);
    Vector3 v3 = new(size.x / 2f, 0, size.y / 2f);
    Vector3 v4 = new(size.x / 2f, 0, -size.y / 2f);

    if (angle != 0f)
    {
      Quaternion q = Quaternion.AngleAxis(angle, Vector3.up);
      v1 = q * v1;
      v2 = q * v2;
      v3 = q * v3;
      v4 = q * v4;
    }

    subMesh.verts.Add(v1 + center);
    subMesh.verts.Add(v2 + center);
    subMesh.verts.Add(v3 + center);
    subMesh.verts.Add(v4 + center);

    if (vertexColors.Length >= 4)
    {
      for (int i = 0; i < 4; i++) subMesh.colors.Add(vertexColors[i]);
    }
    else
    {
      Color32 color32 = color;
      for (int i = 0; i < 4; i++) subMesh.colors.Add(color32);
    }

    if (uvArray.Length >= 4)
    {
      subMesh.uvs.Add(new(uvArray[0].x, uvArray[0].y, 0f));
      subMesh.uvs.Add(new(uvArray[1].x, uvArray[1].y, 0f));
      subMesh.uvs.Add(new(uvArray[2].x, uvArray[2].y, 0f));
      subMesh.uvs.Add(new(uvArray[3].x, uvArray[3].y, 0f));
    }
    else
    {
      subMesh.uvs.Add(new(0f, 0f, 0f));
      subMesh.uvs.Add(new(0f, 1f, 0f));
      subMesh.uvs.Add(new(1f, 1f, 0f));
      subMesh.uvs.Add(new(1f, 0f, 0f));
    }

    for (int i = 0; i < 4; i++) subMesh.normals.Add(Vector3.up);

    subMesh.tris.Add(vCount);
    subMesh.tris.Add(vCount + 1);
    subMesh.tris.Add(vCount + 2);
    subMesh.tris.Add(vCount);
    subMesh.tris.Add(vCount + 2);
    subMesh.tris.Add(vCount + 3);
  }

  public static void PrintDamageScratches(MapDrawLayer layer, IntVec3 c, RoofGraphicData? graphicData, float y,
                                          short hp, short maxHp, float alpha, in RoofPaintOptions opts = default)
  {
    float damagePct = 1f - ((float)hp / maxHp);
    int scratchesCount = 0;
    if (damagePct > 0.75f) scratchesCount = 3;
    else if (damagePct > 0.5f) scratchesCount = 2;
    else if (damagePct > 0.1f) scratchesCount = 1;

    if (scratchesCount <= 0) return;

    IList<Material>? scratchMats = graphicData?.damageData?.scratchMats;
    if (scratchMats == null || scratchMats.Count == 0)
    {
      scratchMats = DefaultScratchMats;
    }

    Rand.PushState(c.GetHashCode());
    for (int i = 0; i < scratchesCount; i++)
    {
      Material scratchMat = AtQueue(scratchMats.RandomElement(), Offset(opts.RenderQueueOverride, 2));
      LayerSubMesh scratchSubMesh = layer.GetSubMesh(scratchMat);
      if (scratchSubMesh == null) continue;

      float rot = Rand.Range(0f, 360f);
      float scale = Rand.Range(0.7f, 0.9f);

      Vector3 center = new(c.x + 0.5f, y, c.z + 0.5f);

      Vector2 size = new(scale, scale);
      Vector3 v1 = new(-size.x / 2f, 0, -size.y / 2f);
      Vector3 v2 = new(-size.x / 2f, 0, size.y / 2f);
      Vector3 v3 = new(size.x / 2f, 0, size.y / 2f);
      Vector3 v4 = new(size.x / 2f, 0, -size.y / 2f);

      Quaternion rotQ = Quaternion.AngleAxis(rot, Vector3.up);
      v1 = rotQ * v1 + center;
      v2 = rotQ * v2 + center;
      v3 = rotQ * v3 + center;
      v4 = rotQ * v4 + center;

      int sVCount = scratchSubMesh.verts.Count;
      scratchSubMesh.verts.Add(v1);
      scratchSubMesh.verts.Add(v2);
      scratchSubMesh.verts.Add(v3);
      scratchSubMesh.verts.Add(v4);

      Color32 scratchColor = new(255, 255, 255, (byte)(alpha * 255));
      for (int j = 0; j < 4; j++) scratchSubMesh.colors.Add(scratchColor);

      scratchSubMesh.uvs.Add(new(0f, 0f, 0f));
      scratchSubMesh.uvs.Add(new(0f, 1f, 0f));
      scratchSubMesh.uvs.Add(new(1f, 1f, 0f));
      scratchSubMesh.uvs.Add(new(1f, 0f, 0f));

      for (int j = 0; j < 4; j++) scratchSubMesh.normals.Add(Vector3.up);

      scratchSubMesh.tris.Add(sVCount);
      scratchSubMesh.tris.Add(sVCount + 1);
      scratchSubMesh.tris.Add(sVCount + 2);
      scratchSubMesh.tris.Add(sVCount);
      scratchSubMesh.tris.Add(sVCount + 2);
      scratchSubMesh.tris.Add(sVCount + 3);
    }
    Rand.PopState();
  }
}
