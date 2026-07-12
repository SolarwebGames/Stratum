using RimWorld;
using UnityEngine;
using Verse;

using SolarWeb.Stratum.Stats;

namespace SolarWeb.Stratum.Graphics;

[StaticConstructorOnStartup]
public class SkylightShadowsRenderer : SectionLayer
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

  public SkylightShadowsRenderer(Section section) : base(section)
  {
    relevantChangeTypes = (ulong)MapMeshFlagDefOf.Roofs | (ulong)MapMeshFlagDefOf.FogOfWar;
  }

  public override bool Visible => true;

  public override void DrawLayer()
  {
    base.DrawLayer();
  }

  public override void Regenerate()
  {
    ClearSubMeshes(MeshParts.All);

    Map map = base.Map;
    if (map == null || map.roofGrid == null || map.fogGrid == null) return;

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
      if (roof == null) continue;

      if (RoofStatCache.IsSkylight(roof))
      {
        float dirt = skylightDirt.GetDirtLevel(c);
        if (dirt > 0.01f)
        {
          Color shadowCol = new(0f, 0f, 0f, dirt * 0.5f);
          Material dMat = DirtMats[Mathf.Abs(c.GetHashCode()) % DirtMats.Length];
          DrawQuadCustom(new Vector3(c.x + 0.5f, baseAltitude + 0.001f, c.z + 0.5f), Vector2.one, dMat, shadowCol, Rot4.North);
        }

        float pollen = skylightDirt.GetPollenLevel(c);
        if (pollen > 0.01f)
        {
          Color shadowCol = new(0f, 0f, 0f, pollen * 0.4f);
          Material pMat = DirtMats[Mathf.Abs(c.GetHashCode()) % DirtMats.Length];
          DrawQuadCustom(new Vector3(c.x + 0.5f, baseAltitude + 0.002f, c.z + 0.5f), Vector2.one, pMat, shadowCol, Rot4.North);
        }

        float snow = skylightDirt.GetSnowLevel(c);
        if (snow > 0.01f)
        {
          Color shadowCol = new(0f, 0f, 0f, snow * 0.6f);
          DrawQuadCustom(new Vector3(c.x + 0.5f, baseAltitude + 0.003f, c.z + 0.5f), Vector2.one, SnowMat, shadowCol, Rot4.North);
        }
      }
    }

    FinalizeMesh(MeshParts.All);
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
