using RimWorld;
using UnityEngine;
using Verse;
using System.Collections.Generic;

using SolarWeb.Stratum.Stats;
using SolarWeb.Stratum.MapComponents;
using SolarWeb.Stratum.Utilities;

namespace SolarWeb.Stratum.Graphics;

[StaticConstructorOnStartup]
public class CustomRoofsRenderer : SectionLayer
{
  private static Material[]? defaultScratchMats;
  private static Material[] DefaultScratchMats
  {
    get
    {
      if (defaultScratchMats == null)
      {
        defaultScratchMats = [
          MaterialPool.MatFrom(RimWorldTextures.Damage.Scratch1, ShaderDatabase.Cutout),
            MaterialPool.MatFrom(RimWorldTextures.Damage.Scratch2, ShaderDatabase.Cutout),
            MaterialPool.MatFrom(RimWorldTextures.Damage.Scratch3, ShaderDatabase.Cutout)
        ];
      }
      return defaultScratchMats;
    }
  }

  private static Material? fallbackMat;
  private static Material FallbackMat => fallbackMat ??= MaterialPool.MatFrom(RimWorldTextures.Terrain.Surfaces.Concrete, ShaderDatabase.Cutout, new Color(0.5f, 0.5f, 0.5f));
  public CustomRoofsRenderer(Section section) : base(section)
  {
    relevantChangeTypes = (ulong)MapMeshFlagDefOf.Roofs | (ulong)MapMeshFlagDefOf.Buildings | (ulong)MapMeshFlagDefOf.FogOfWar;
  }

  // Always return true so the mesh regenerates in the background even if the overlay is hidden
  public override bool Visible => true;

  public override void DrawLayer()
  {
    if (Find.PlaySettings.showRoofOverlay)
    {
      base.DrawLayer();
    }
  }

  public override void Regenerate()
  {
    ClearSubMeshes(MeshParts.All);

    Map map = base.Map;
    if (map == null || map.roofGrid == null || map.fogGrid == null) return;

    var integrityGrid = map.GetComponent<RoofIntegrityGrid>();
    if (integrityGrid != null && !integrityGrid.hasScanned && Visible)
    {
      integrityGrid.ExecuteScan();
    }

    CellRect cellRect = new(section.botLeft.x, section.botLeft.z, 17, 17);
    cellRect.ClipInsideMap(map);

    bool isCutscene = false;
    CellRect captureBounds = CellRect.Empty;
    if (GravshipCapturer.IsGravshipRenderInProgress)
    {
      captureBounds = GravshipCapturer.GravshipCaptureBounds;
    }
    else
    {
      isCutscene = WorldComponent_GravshipController.CutsceneInProgress && !GravshipCapturer.IsGravshipRenderInProgress && map == Find.CurrentMap;
      captureBounds = GravshipCapturer.GravshipCaptureBounds;
    }

    // Use MapDataOverlay to ensure we draw above the lighting overlay, 
    // but leave MetaOverlays available for ghost placement so we don't z-fight.
    float altitude = AltitudeLayer.MapDataOverlay.AltitudeFor();

    foreach (IntVec3 c in cellRect)
    {
      if (map.fogGrid.IsFogged(c)) continue;
      if (isCutscene && captureBounds.Contains(c)) continue;

      RoofDef roof = map.roofGrid.RoofAt(c);
      if (roof == null || !RoofStatCache.IsCustomRoof(roof)) continue;

      var myGraphicData = RoofStatCache.GetGraphicData(roof);
      ThingDef? stuff = integrityGrid?.GetStuff(c);
      Color roofColor = RoofStatCache.GetColor(roof, stuff);
      float alpha = 1f;

      if (RoofStatCache.IsSkylight(roof))
      {
        alpha = 1f - RoofStatCache.GetTransparency(roof);
      }
      roofColor.a *= alpha;

      if (myGraphicData != null)
      {
        var entry = RoofAtlasManager.GetEntry(myGraphicData.texPath);
        var (cutout, transparent) = RoofAtlasManager.GetMaterials(myGraphicData.texPath, roofColor);

        bool isTransparent = RoofStatCache.IsSkylight(roof);
        Material mat = isTransparent ? transparent : cutout;

        if (entry.IsSeamless && entry.SeamlessGrid != null)
        {
          int col = c.x % entry.GridWidth;
          if (col < 0) col += entry.GridWidth;

          int row = c.z % entry.GridHeight;
          if (row < 0) row += entry.GridHeight;

          if (entry.SeamlessGrid.TryGetValue((col, row), out var uvs))
          {
            if (RoofStatCache.IsSkylight(roof) && myGraphicData.skylightFrameWidth > 0f)
            {
              DrawFramedSkylight(c, roof, myGraphicData, altitude, stuff, uvs, myGraphicData.texPath);
            }
            else
            {
              DrawQuadCustom(new Vector3(c.x + 0.5f, altitude, c.z + 0.5f), Vector2.one, mat, Color.white, 0f, uvs);
            }
          }
        }
        else
        {
          var uvs = entry.FlatVariants[Mathf.Abs(c.GetHashCode()) % entry.FlatVariants.Count];
          if (RoofStatCache.IsSkylight(roof) && myGraphicData.skylightFrameWidth > 0f)
          {
            DrawFramedSkylight(c, roof, myGraphicData, altitude, stuff, uvs, myGraphicData.texPath);
          }
          else
          {
            DrawQuadCustom(new Vector3(c.x + 0.5f, altitude, c.z + 0.5f), Vector2.one, mat, Color.white, 0f, uvs);
          }
        }
      }
      else
      {
        DrawQuadCustom(new Vector3(c.x + 0.5f, altitude, c.z + 0.5f), Vector2.one, FallbackMat, roofColor, 0f);
      }

      if (integrityGrid != null)
      {
        short hp = integrityGrid.GetHitPoints(c);
        short maxHp = (short)RoofStatCache.GetMaxHitPoints(roof, stuff);

        if (hp > 0 && hp < maxHp)
        {
          // Scratches slightly above roof (+0.05f)
          DrawDamageScratches(c, roof, myGraphicData, altitude + 0.05f, hp, maxHp, alpha);
        }
      }
    }

    FinalizeMesh(MeshParts.All);
  }

  private void DrawFramedSkylight(IntVec3 c, RoofDef roof, RoofGraphicData gd, float y, ThingDef? stuff, Vector2[] uv, string texPath)
  {
    float f = gd.skylightFrameWidth;
    float glassAlpha = 1f - RoofStatCache.GetTransparency(roof);

    Color frameColor = RoofStatCache.GetColor(roof, stuff);
    frameColor.a = 1f;

    Color glassColor = RoofStatCache.GetGlassTint(roof, Map, c);
    glassColor.a = glassAlpha;

    // Fetch materials specific to their colors
    Material frameMat = RoofAtlasManager.GetMaterials(texPath, frameColor).cutout;
    Material glassMat = RoofAtlasManager.GetMaterials(texPath, glassColor).transparent;

    Vector3 basePos = new(c.x, y, c.z);

    // 9-slice positions (0.0 to 1.0)
    System.ReadOnlySpan<float> p = stackalloc float[] { 0f, f, 1f - f, 1f };

    // UVs (interpolate between corner UVs)
    Vector2 bl = uv[0], tl = uv[1], tr = uv[2], br = uv[3];

    for (int x = 0; x < 3; x++)
    {
      for (int z = 0; z < 3; z++)
      {
        bool isCenter = (x == 1 && z == 1);
        Material quadMat = isCenter ? glassMat : frameMat;

        Vector3 qCenter = basePos + new Vector3((p[x] + p[x + 1]) / 2f, 0, (p[z] + p[z + 1]) / 2f);
        Vector2 qSize = new(p[x + 1] - p[x], p[z + 1] - p[z]);

        // Compute UVs for this slice
        System.Span<Vector2> qUv = stackalloc Vector2[4];

        Vector2 GetUv(float px, float pz)
        {
          Vector2 bottom = Vector2.Lerp(bl, br, px);
          Vector2 top = Vector2.Lerp(tl, tr, px);
          return Vector2.Lerp(bottom, top, pz);
        }

        qUv[0] = GetUv(p[x], p[z]);     // BL
        qUv[1] = GetUv(p[x], p[z + 1]);   // TL
        qUv[2] = GetUv(p[x + 1], p[z + 1]); // TR
        qUv[3] = GetUv(p[x + 1], p[z]);   // BR

        // Vertex color is white because color is baked into the material
        DrawQuadCustom(qCenter, qSize, quadMat, Color.white, 0f, qUv);
      }
    }
  }

  private void DrawQuadCustom(Vector3 center, Vector2 size, Material mat, Color color, float angle = 0f, System.ReadOnlySpan<Vector2> uvArray = default, Color[]? vertexColors = null)
  {
    LayerSubMesh subMesh = GetSubMesh(mat);
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

    if (vertexColors != null && vertexColors.Length >= 4)
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

  private void DrawDamageScratches(IntVec3 c, RoofDef roof, RoofGraphicData? graphicData, float y, short hp, short maxHp, float alpha)
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
      Material scratchMat = scratchMats.RandomElement();
      LayerSubMesh scratchSubMesh = GetSubMesh(scratchMat);

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
