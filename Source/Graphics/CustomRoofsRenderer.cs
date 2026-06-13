using RimWorld;
using UnityEngine;
using Verse;
using SolarWeb.Stratum.Stats;
using SolarWeb.Stratum.MapComponents;
using System.Collections.Generic;

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

    var integrityGrid = map.GetComponent<RoofIntegrityGrid>();
    if (integrityGrid != null && !integrityGrid.hasScanned && Visible)
    {
      integrityGrid.ExecuteScan();
    }

    RoofGrid roofGrid = map.roofGrid;
    FogGrid fogGrid = map.fogGrid;
    CellRect cellRect = section.CellRect;

    bool isCutscene = false;
    CellRect captureBounds = default;
    if (ModsConfig.OdysseyActive)
    {
      isCutscene = WorldComponent_GravshipController.CutsceneInProgress && !GravshipCapturer.IsGravshipRenderInProgress && map == Find.CurrentMap;
      captureBounds = GravshipCapturer.GravshipCaptureBounds;
    }

    // Use MoteOverhead to ensure we are below Skyfallers (30) but above Blueprints (26)
    float altitude = AltitudeLayer.MoteOverhead.AltitudeFor();

    // PASS 1: Draw all roofs and damage scratches
    foreach (IntVec3 c in cellRect)
    {
      if (fogGrid.IsFogged(c)) continue;
      if (isCutscene && captureBounds.Contains(c)) continue;

      RoofDef roof = roofGrid.RoofAt(c);
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
        bool gotUv = myGraphicData.isSeamless
          ? RoofAtlasManager.TryGetSeamlessUv(myGraphicData.texPath, c.x, c.z, out var uv, out var mat)
          : RoofAtlasManager.TryGetUv(myGraphicData.texPath, c.GetHashCode(), out uv, out mat);

        if (gotUv)
        {
          if (RoofStatCache.IsSkylight(roof) && myGraphicData.skylightFrameWidth > 0f)
          {
            DrawFramedSkylight(c, roof, myGraphicData, altitude, stuff, uv!, mat!);
          }
          else
          {
            DrawQuadCustom(new Vector3(c.x + 0.5f, altitude, c.z + 0.5f), Vector2.one, mat!, roofColor, Rot4.North, uv!);
          }
        }
        else
        {
          DrawQuadCustom(new Vector3(c.x + 0.5f, altitude, c.z + 0.5f), Vector2.one, FallbackMat, roofColor, Rot4.North);
        }
      }
      else
      {
        DrawQuadCustom(new Vector3(c.x + 0.5f, altitude, c.z + 0.5f), Vector2.one, FallbackMat, roofColor, Rot4.North);
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

  private void DrawFramedSkylight(IntVec3 c, RoofDef roof, RoofGraphicData gd, float y, ThingDef? stuff, Vector2[] uv, Material mat)
  {
    float f = gd.skylightFrameWidth;
    float glassAlpha = 1f - RoofStatCache.GetTransparency(roof);

    Color frameColor = RoofStatCache.GetColor(roof, stuff);
    frameColor.a = 1f;

    Color glassColor = RoofStatCache.GetGlassTint(roof, Map, c);
    glassColor.a = glassAlpha;

    Vector3 basePos = new Vector3(c.x, y, c.z);

    // 9-slice positions (0.0 to 1.0)
    float[] p = { 0f, f, 1f - f, 1f };

    // UVs (interpolate between corner UVs)
    Vector2 bl = uv[0], tl = uv[1], tr = uv[2], br = uv[3];

    for (int x = 0; x < 3; x++)
    {
      for (int z = 0; z < 3; z++)
      {
        bool isCenter = (x == 1 && z == 1);
        Color quadColor = isCenter ? glassColor : frameColor;

        Vector3 qCenter = basePos + new Vector3((p[x] + p[x + 1]) / 2f, 0, (p[z] + p[z + 1]) / 2f);
        Vector2 qSize = new Vector2(p[x + 1] - p[x], p[z + 1] - p[z]);

        // Compute UVs for this slice
        Vector2[] qUv = new Vector2[4];

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

        DrawQuadCustom(qCenter, qSize, mat, quadColor, Rot4.North, qUv);
      }
    }
  }

  private void DrawQuadCustom(Vector3 center, Vector2 size, Material mat, Color color, Rot4 rot, Vector2[]? uvArray = null, Color[]? vertexColors = null)
  {
    LayerSubMesh subMesh = GetSubMesh(mat);
    int vCount = subMesh.verts.Count;

    Vector3 v1 = new Vector3(-size.x / 2f, 0, -size.y / 2f);
    Vector3 v2 = new Vector3(-size.x / 2f, 0, size.y / 2f);
    Vector3 v3 = new Vector3(size.x / 2f, 0, size.y / 2f);
    Vector3 v4 = new Vector3(size.x / 2f, 0, -size.y / 2f);

    if (rot != Rot4.North)
    {
      Quaternion q = Quaternion.AngleAxis(rot.AsAngle, Vector3.up);
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

    if (uvArray != null && uvArray.Length >= 4)
    {
      subMesh.uvs.Add(new Vector3(uvArray[0].x, uvArray[0].y, 0f));
      subMesh.uvs.Add(new Vector3(uvArray[1].x, uvArray[1].y, 0f));
      subMesh.uvs.Add(new Vector3(uvArray[2].x, uvArray[2].y, 0f));
      subMesh.uvs.Add(new Vector3(uvArray[3].x, uvArray[3].y, 0f));
    }
    else
    {
      subMesh.uvs.Add(new Vector3(0f, 0f, 0f));
      subMesh.uvs.Add(new Vector3(0f, 1f, 0f));
      subMesh.uvs.Add(new Vector3(1f, 1f, 0f));
      subMesh.uvs.Add(new Vector3(1f, 0f, 0f));
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

      Vector3 center = new Vector3(c.x + 0.5f, y, c.z + 0.5f);

      Vector2 size = new Vector2(scale, scale);
      Vector3 v1 = new Vector3(-size.x / 2f, 0, -size.y / 2f);
      Vector3 v2 = new Vector3(-size.x / 2f, 0, size.y / 2f);
      Vector3 v3 = new Vector3(size.x / 2f, 0, size.y / 2f);
      Vector3 v4 = new Vector3(size.x / 2f, 0, -size.y / 2f);

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

      Color32 scratchColor = new Color32(255, 255, 255, (byte)(alpha * 255));
      for (int j = 0; j < 4; j++) scratchSubMesh.colors.Add(scratchColor);

      scratchSubMesh.uvs.Add(new Vector3(0f, 0f, 0f));
      scratchSubMesh.uvs.Add(new Vector3(0f, 1f, 0f));
      scratchSubMesh.uvs.Add(new Vector3(1f, 1f, 0f));
      scratchSubMesh.uvs.Add(new Vector3(1f, 0f, 0f));

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
