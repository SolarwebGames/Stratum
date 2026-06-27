using RimWorld;
using UnityEngine;
using Verse;
using System.Collections.Generic;

using SolarWeb.Stratum.Stats;
using SolarWeb.Stratum.Utilities;

namespace SolarWeb.Stratum.Graphics;

[StaticConstructorOnStartup]
public class CustomRoofBuildingsRenderer : SectionLayer
{
  private static readonly Vector2[] FlippedUvs =
  [
    new(1f, 0f),
    new(1f, 1f),
    new(0f, 1f),
    new(0f, 0f)
  ];


  public CustomRoofBuildingsRenderer(Section section) : base(section)
  {
    relevantChangeTypes = (ulong)MapMeshFlagDefOf.Buildings | (ulong)MapMeshFlagDefOf.Roofs | (ulong)MapMeshFlagDefOf.FogOfWar;
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
    if (map == null || map.fogGrid == null || map.thingGrid == null) return;

    CellRect cellRect = new(section.botLeft.x, section.botLeft.z, 17, 17);
    cellRect.ClipInsideMap(map);

    foreach (IntVec3 c in cellRect)
    {
      if (map.fogGrid.IsFogged(c)) continue;

      var things = map.thingGrid.ThingsListAt(c);
      for (int i = 0; i < things.Count; i++)
      {
        Thing thing = things[i];
        if (RoofBuildings.IsRoofBuildingOrBlueprintOrFrame(thing) && RoofBuildings.ShouldRenderRoofBuilding(thing))
        {
          if (thing.Graphic != null && !(thing is Frame) && !(thing is Blueprint))
          {
            Material baseMat = thing.Graphic.MatAt(thing.Rotation, thing);
            if (baseMat != null && baseMat.mainTexture != null)
            {
              Vector2 size;
              bool flipped = false;
              if (thing.Graphic.ShouldDrawRotated)
              {
                size = thing.Graphic.drawSize;
              }
              else
              {
                size = thing.Rotation.IsHorizontal ? thing.Graphic.drawSize.Rotated() : thing.Graphic.drawSize;
                flipped = (thing.Rotation == Rot4.West && thing.Graphic.WestFlipped) ||
                          (thing.Rotation == Rot4.East && thing.Graphic.EastFlipped);
              }

              if (thing.MultipleItemsPerCellDrawn())
              {
                size *= 0.8f;
              }

              float angle = 0f;
              if (thing.Graphic.ShouldDrawRotated)
              {
                angle = thing.Rotation.AsAngle;
              }
              if (flipped && thing.Graphic.data != null)
              {
                angle += thing.Graphic.data.flipExtraRotation;
              }

              Vector2[]? uvs = flipped ? FlippedUvs : null;
              DrawQuadCustom(thing.DrawPos, size, baseMat, Color.white, angle, uvs);
            }
          }
        }
      }
    }

    FinalizeMesh(MeshParts.All);
  }

  private void DrawQuadCustom(Vector3 center, Vector2 size, Material mat, Color color, float angle = 0f, Vector2[]? uvArray = null, Color[]? vertexColors = null)
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

    if (uvArray != null && uvArray.Length >= 4)
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
}
