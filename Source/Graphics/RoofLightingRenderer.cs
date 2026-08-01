using RimWorld;
using UnityEngine;
using Verse;

namespace SolarWeb.Stratum.Graphics;

[StaticConstructorOnStartup]
public class RoofLightingRenderer : SectionLayer
{
  private static Material? skylightTintMat;

  private static Material SkylightTintMat
  {
    get
    {
      if (skylightTintMat == null)
      {
        skylightTintMat = new Material(MatBases.LightOverlay.shader)
        {
          mainTexture = BaseContent.WhiteTex,
          renderQueue = 3162
        };
      }
      return skylightTintMat;
    }
  }

  public RoofLightingRenderer(Section section) : base(section)
  {
    relevantChangeTypes = MapMeshFlagDefOf.Roofs | MapMeshFlagDefOf.Buildings;
  }

  public override bool Visible => true;

  public override void DrawLayer()
  {
    if (skylightTintMat != null)
    {
      skylightTintMat.color = MatBases.LightOverlay.color;
    }
    base.DrawLayer();
  }

  public override void Regenerate()
  {
    ClearSubMeshes(MeshParts.All);

    Map map = section.map;
    if (map == null || map.roofGrid == null) return;

    CellRect rect = section.CellRect;
    if (rect.Width <= 0 || rect.Height <= 0) return;
    if (!SkylightOverlayCompositor.TryBuildTintSection(map, rect)) return;

    LayerSubMesh tintMesh = GetSubMesh(SkylightTintMat);
    float y = AltitudeLayer.LightingOverlay.AltitudeFor() + 0.002f;

    for (int z = rect.minZ; z <= rect.maxZ; z++)
    {
      for (int x = rect.minX; x <= rect.maxX; x++)
      {
        if (!SkylightOverlayCompositor.IsSkylightCell(x, z)) continue;

        Color32 cBL = SkylightOverlayCompositor.GetCornerTint(x, z);
        Color32 cBR = SkylightOverlayCompositor.GetCornerTint(x + 1, z);
        Color32 cTL = SkylightOverlayCompositor.GetCornerTint(x, z + 1);
        Color32 cTR = SkylightOverlayCompositor.GetCornerTint(x + 1, z + 1);

        if (HasTint(cBL) || HasTint(cBR) || HasTint(cTL) || HasTint(cTR))
        {
          AppendQuadColored(tintMesh, x, z, x + 1f, z + 1f, y, cBL, cTL, cTR, cBR);
        }
      }
    }

    if (tintMesh.verts.Count > 0)
    {
      tintMesh.finalized = false;
      tintMesh.FinalizeMesh(MeshParts.All);
    }
  }

  private static bool HasTint(Color32 c) => c.r < 255 || c.g < 255 || c.b < 255;

  private static void AppendQuadColored(LayerSubMesh sm, float minX, float minZ, float maxX, float maxZ, float y,
                                        Color32 cBL, Color32 cTL, Color32 cTR, Color32 cBR)
  {
    int i = sm.verts.Count;
    sm.verts.Add(new Vector3(minX, y, minZ)); // botLeft
    sm.verts.Add(new Vector3(minX, y, maxZ)); // topLeft
    sm.verts.Add(new Vector3(maxX, y, maxZ)); // topRight
    sm.verts.Add(new Vector3(maxX, y, minZ)); // botRight

    sm.colors.Add(cBL);
    sm.colors.Add(cTL);
    sm.colors.Add(cTR);
    sm.colors.Add(cBR);

    sm.tris.Add(i);
    sm.tris.Add(i + 1);
    sm.tris.Add(i + 2);
    sm.tris.Add(i);
    sm.tris.Add(i + 2);
    sm.tris.Add(i + 3);
  }
}
