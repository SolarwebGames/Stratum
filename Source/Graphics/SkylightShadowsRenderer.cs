using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

using SolarWeb.Stratum.Stats;

namespace SolarWeb.Stratum.Graphics;

[StaticConstructorOnStartup]
public class SkylightShadowsRenderer : SectionLayer_Dynamic
{
  private static Material[]? dirtMats;
  private static Material[] DirtMats => dirtMats ??= LoadMatsFromDef(ThingDefOf.Filth_Dirt);

  private static Material? snowMat;
  private static Material SnowMat => snowMat ??= MaterialPool.MatFrom((Texture2D)MatBases.Snow.mainTexture, ShaderDatabase.Transparent, Color.white);

  private static Material[] LoadMatsFromDef(ThingDef def)
  {
    if (def?.graphic is Graphic_Collection collection)
    {
      var field = HarmonyLib.AccessTools.Field(typeof(Graphic_Collection), "subGraphics");
      if (field?.GetValue(collection) is Graphic[] array && array.Length > 0)
      {
        Material[] mats = new Material[array.Length];
        for (int i = 0; i < array.Length; i++)
        {
          mats[i] = MaterialPool.MatFrom(array[i].path, ShaderDatabase.Transparent, Color.white);
        }
        return mats;
      }
    }
    return [BaseContent.BadMat];
  }

  private Vector2 lastShadowOffset = new(-9999f, -9999f);
  private int lastBuildingsVersion = -1;
  private static readonly Dictionary<int, int> buildingsVersionByMap = new();

  private static int lastFrame = -1;
  private static readonly Dictionary<int, CellRect> cachedRects = new();

  public SkylightShadowsRenderer(Section section) : base(section)
  {
    relevantChangeTypes = (ulong)MapMeshFlagDefOf.Roofs | (ulong)MapMeshFlagDefOf.FogOfWar;
  }

  public override bool Visible => Stratum.Settings.enableSkylightShadows;

  public override bool ShouldDrawDynamic(CellRect view)
  {
    Map map = Map;
    if (map == null) return false;

    Vector2 sunShadowVec = GenCelestial.GetLightSourceInfo(map, GenCelestial.LightType.Shadow).vector;
    Vector2 offset = sunShadowVec * 0.12f;

    CellRect expandedRect = GetShadowsViewRect(map, view, offset);
    return section.CellRect.Overlaps(expandedRect);
  }

  private static CellRect GetShadowsViewRect(Map map, CellRect rect, Vector2 offset)
  {
    int currentFrame = RealTime.frameCount;
    int mapId = map.uniqueID;
    if (lastFrame == currentFrame && cachedRects.TryGetValue(mapId, out CellRect cached))
    {
      return cached;
    }

    if (offset.x < 0f)
    {
      rect.maxX -= Mathf.FloorToInt(offset.x);
    }
    else
    {
      rect.minX -= Mathf.CeilToInt(offset.x);
    }

    if (offset.y < 0f)
    {
      rect.maxZ -= Mathf.FloorToInt(offset.y);
    }
    else
    {
      rect.minZ -= Mathf.CeilToInt(offset.y);
    }

    lastFrame = currentFrame;
    cachedRects[mapId] = rect.ClipInsideMap(map);
    return cachedRects[mapId];
  }

  public override void DrawLayer()
  {
    if (!Visible) return;

    Map map = Map;
    if (map == null) return;

    TryRebuildShadows();

    float shadowStrength = GenCelestial.CurShadowStrength(map);
    bool isDay = GenCelestial.IsDaytime(GenCelestial.CurCelestialSunGlow(map));
    if (!isDay)
    {
      shadowStrength *= 0.3f;
    }

    if (shadowStrength <= 0.01f) return;

    var propertyBlock = new MaterialPropertyBlock();
    propertyBlock.SetColor("_Color", new Color(1f, 1f, 1f, shadowStrength));

    int count = subMeshes.Count;
    for (int i = 0; i < count; i++)
    {
      LayerSubMesh subMesh = subMeshes[i];
      if (subMesh.finalized && !subMesh.disabled && subMesh.verts.Count > 0)
      {
        UnityEngine.Graphics.DrawMesh(subMesh.mesh, Matrix4x4.identity, subMesh.material, subMesh.renderLayer, null, 0, propertyBlock);
      }
    }
  }

  private void TryRebuildShadows()
  {
    Map map = Map;
    if (map == null || map.roofGrid == null) return;

    Vector2 sunShadowVec = GenCelestial.GetLightSourceInfo(map, GenCelestial.LightType.Shadow).vector;
    Vector2 offset = sunShadowVec * 0.12f;

    int mapId = map.uniqueID;
    int currentBuildingsVersion = buildingsVersionByMap.TryGetValue(mapId, out int v) ? v : 0;

    if (lastBuildingsVersion == currentBuildingsVersion && (offset - lastShadowOffset).sqrMagnitude < 0.04f * 0.04f)
    {
      return;
    }

    RebuildShadowMesh(map, offset);

    lastShadowOffset = offset;
    lastBuildingsVersion = currentBuildingsVersion;
  }

  public override void Regenerate()
  {
    ClearSubMeshes(MeshParts.All);
    Map map = Map;
    if (map == null) return;

    buildingsVersionByMap[map.uniqueID] = (buildingsVersionByMap.TryGetValue(map.uniqueID, out int v) ? v : 0) + 1;
    lastShadowOffset = new Vector2(-9999f, -9999f);
  }

  private void RebuildShadowMesh(Map map, Vector2 offset)
  {
    ClearSubMeshes(MeshParts.All);
    if (!Stratum.Settings.enableSkylightShadows) return;

    var skylightDirt = map.GetComponent<MapComponents.SkylightCoating>();
    if (skylightDirt == null) return;

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

    float baseAltitude = AltitudeLayer.Shadows.AltitudeFor() + 0.005f;

    foreach (IntVec3 c in cellRect)
    {
      if (fogGrid.IsFogged(c)) continue;
      if (isCutscene && captureBounds.Contains(c)) continue;

      RoofDef roof = roofGrid.RoofAt(c);
      if (roof == null || !RoofStatCache.IsSkylight(roof)) continue;

      Building edifice = c.GetEdifice(map);
      if (edifice != null && edifice.def.staticSunShadowHeight > 0f) continue;

      IntVec3 landCell = new(Mathf.FloorToInt(c.x + 0.5f + offset.x), 0, Mathf.FloorToInt(c.z + 0.5f + offset.y));
      if (!landCell.InBounds(map) || !GenSight.LineOfSight(c, landCell, map, skipFirstCell: true))
      {
        continue;
      }

      if (Stratum.Settings.enableDirtGraphics)
      {
        float dirt = skylightDirt.GetDirtLevel(c);
        if (dirt > 0.01f)
        {
          Color shadowCol = new(0f, 0f, 0f, dirt * 0.10f);
          Material dMat = DirtMats[Mathf.Abs(c.GetHashCode()) % DirtMats.Length];
          DrawShadowElement(c, baseAltitude + 0.001f, dMat, shadowCol, 3123512, offset);
        }
      }

      if (Stratum.Settings.enablePollenGraphics)
      {
        float pollen = skylightDirt.GetPollenLevel(c);
        if (pollen > 0.01f)
        {
          Color shadowCol = new(0f, 0f, 0f, pollen * 0.10f);
          Material pMat = DirtMats[Mathf.Abs(c.GetHashCode()) % DirtMats.Length];
          DrawShadowElement(c, baseAltitude + 0.002f, pMat, shadowCol, 9845123, offset);
        }
      }

      if (Stratum.Settings.enableSnowGraphics)
      {
        float snow = skylightDirt.GetSnowLevel(c);
        if (snow > 0.01f)
        {
          Color shadowCol = new(0f, 0f, 0f, snow * 0.30f);
          DrawQuadCustom(new Vector3(c.x + 0.5f + offset.x, baseAltitude + 0.003f, c.z + 0.5f + offset.y), Vector2.one * 1.15f, SnowMat, shadowCol, Rot4.North);
        }
      }
    }

    FinalizeMesh(MeshParts.All);
  }

  private void DrawShadowElement(IntVec3 c, float finalAltitude, Material mat, Color shadowColor, int seedOffset, Vector2 offset, float shadowScaleMultiplier = 1.15f)
  {
    Rand.PushState(c.GetHashCode() ^ seedOffset);
    float offsetX = Rand.Range(-0.15f, 0.15f);
    float offsetZ = Rand.Range(-0.15f, 0.15f);
    float scaleX = Rand.Range(0.8f, 1.2f) * shadowScaleMultiplier;
    float scaleZ = Rand.Range(0.8f, 1.2f) * shadowScaleMultiplier;
    Rot4 rot = new(Rand.RangeInclusive(0, 3));
    bool flipUv = Rand.Value < 0.5f;
    Rand.PopState();

    Vector2[]? uvArray = flipUv ? [new(1f, 0f), new(1f, 1f), new(0f, 1f), new(0f, 0f)] : null;
    DrawQuadCustom(new Vector3(c.x + 0.5f + offsetX + offset.x, finalAltitude, c.z + 0.5f + offsetZ + offset.y), new Vector2(scaleX, scaleZ), mat, shadowColor, rot, uvArray);
  }

  private void DrawQuadCustom(Vector3 center, Vector2 size, Material mat, Color color, Rot4 rot, Vector2[]? uvArray = null, Color[]? vertexColors = null)
  {
    LayerSubMesh subMesh = GetSubMesh(mat);
    int vCount = subMesh.verts.Count;

    Vector3 v1 = new(-size.x / 2f, 0f, -size.y / 2f);
    Vector3 v2 = new(-size.x / 2f, 0f, size.y / 2f);
    Vector3 v3 = new(size.x / 2f, 0f, size.y / 2f);
    Vector3 v4 = new(size.x / 2f, 0f, -size.y / 2f);

    if (rot == Rot4.East)
    {
      v1 = new Vector3(-size.x / 2f, 0f, size.y / 2f);
      v2 = new Vector3(size.x / 2f, 0f, size.y / 2f);
      v3 = new Vector3(size.x / 2f, 0f, -size.y / 2f);
      v4 = new Vector3(-size.x / 2f, 0f, -size.y / 2f);
    }
    else if (rot == Rot4.South)
    {
      v1 = new Vector3(size.x / 2f, 0f, size.y / 2f);
      v2 = new Vector3(size.x / 2f, 0f, -size.y / 2f);
      v3 = new Vector3(-size.x / 2f, 0f, -size.y / 2f);
      v4 = new Vector3(-size.x / 2f, 0f, size.y / 2f);
    }
    else if (rot == Rot4.West)
    {
      v1 = new Vector3(size.x / 2f, 0f, -size.y / 2f);
      v2 = new Vector3(-size.x / 2f, 0f, -size.y / 2f);
      v3 = new Vector3(-size.x / 2f, 0f, size.y / 2f);
      v4 = new Vector3(size.x / 2f, 0f, size.y / 2f);
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

    subMesh.tris.Add(vCount);
    subMesh.tris.Add(vCount + 1);
    subMesh.tris.Add(vCount + 2);
    subMesh.tris.Add(vCount);
    subMesh.tris.Add(vCount + 2);
    subMesh.tris.Add(vCount + 3);
  }
}
