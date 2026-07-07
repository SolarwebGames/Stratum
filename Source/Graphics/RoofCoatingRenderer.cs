using RimWorld;
using UnityEngine;
using Verse;

using SolarWeb.Stratum.Stats;

namespace SolarWeb.Stratum.Graphics;

[StaticConstructorOnStartup]
public class RoofCoatingRenderer : SectionLayer
{
  private static Material[]? dirtMats;
  private static Material[] DirtMats => dirtMats ??= LoadMatsFromDef(ThingDefOf.Filth_Dirt);

  private static Material? snowMat;
  private static Material SnowMat
  {
    get
    {
      if (snowMat == null)
      {
        snowMat = MaterialPool.MatFrom((Texture2D)MatBases.Snow.mainTexture, ShaderDatabase.MetaOverlay, Color.white);
        snowMat.renderQueue = 4550;
      }
      return snowMat;
    }
  }

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

  public RoofCoatingRenderer(Section section) : base(section)
  {
    relevantChangeTypes = (ulong)MapMeshFlagDefOf.Roofs | (ulong)MapMeshFlagDefOf.FogOfWar;
  }

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

    float altitude = AltitudeLayer.MoteOverhead.AltitudeFor();

    float GetSnow(int x, int z)
    {
      IntVec3 temp = new(x, 0, z);
      if (!temp.InBounds(map)) return 0f;
      RoofDef r = map.roofGrid.RoofAt(temp);
      if (r == null || !RoofStatCache.IsVisibleRoof(r)) return 0f;
      return skylightDirt.GetSnowLevel(temp);
    }

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
          Color dirtCol = skylightDirt.GetDirtColor(c);
          dirtCol.a = dirt * 0.95f;
          Material dMat = DirtMats[Mathf.Abs(c.GetHashCode()) % DirtMats.Length];
          DrawQuadCustom(new Vector3(c.x + 0.5f, altitude + 0.02f, c.z + 0.5f), Vector2.one, dMat, dirtCol, Rot4.North);
        }
      }

      if (RoofStatCache.IsVisibleRoof(roof))
      {
        float alphaBL = (GetSnow(c.x, c.z) + GetSnow(c.x - 1, c.z) + GetSnow(c.x, c.z - 1) + GetSnow(c.x - 1, c.z - 1)) / 4f;
        float alphaTL = (GetSnow(c.x, c.z) + GetSnow(c.x - 1, c.z) + GetSnow(c.x, c.z + 1) + GetSnow(c.x - 1, c.z + 1)) / 4f;
        float alphaTR = (GetSnow(c.x, c.z) + GetSnow(c.x + 1, c.z) + GetSnow(c.x, c.z + 1) + GetSnow(c.x + 1, c.z + 1)) / 4f;
        float alphaBR = (GetSnow(c.x, c.z) + GetSnow(c.x + 1, c.z) + GetSnow(c.x, c.z - 1) + GetSnow(c.x + 1, c.z - 1)) / 4f;

        if (alphaBL > 0.01f || alphaTL > 0.01f || alphaTR > 0.01f || alphaBR > 0.01f)
        {
          Color[] vColors =
          [
            new(1f, 1f, 1f, alphaBL * 0.95f),
            new(1f, 1f, 1f, alphaTL * 0.95f),
            new(1f, 1f, 1f, alphaTR * 0.95f),
            new(1f, 1f, 1f, alphaBR * 0.95f),
          ];

          if (RoofAtlasManager.uvMap.TryGetValue("Snow", out var entry))
          {
            var (_, transparent) = RoofAtlasManager.GetMaterials("Snow", Color.white);
            transparent.renderQueue = 4550;

            Vector2[]? uv = null;
            if (entry.IsSeamless && entry.SeamlessGrid != null)
            {
              int col = c.x % entry.GridWidth;
              if (col < 0) col += entry.GridWidth;

              int row = c.z % entry.GridHeight;
              if (row < 0) row += entry.GridHeight;

              entry.SeamlessGrid.TryGetValue((col, row), out uv);
            }
            else if (entry.FlatVariants.Count > 0)
            {
              uv = entry.FlatVariants[Mathf.Abs(c.GetHashCode()) % entry.FlatVariants.Count];
            }

            if (uv != null)
            {
              DrawQuadCustom(new Vector3(c.x + 0.5f, altitude + 0.03f, c.z + 0.5f), Vector2.one, transparent, Color.white, Rot4.North, uv, vColors);
            }
            else
            {
              DrawQuadCustom(new Vector3(c.x + 0.5f, altitude + 0.03f, c.z + 0.5f), Vector2.one, SnowMat, Color.white, Rot4.North, null, vColors);
            }
          }
          else
          {
            DrawQuadCustom(new Vector3(c.x + 0.5f, altitude + 0.03f, c.z + 0.5f), Vector2.one, SnowMat, Color.white, Rot4.North, null, vColors);
          }
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
