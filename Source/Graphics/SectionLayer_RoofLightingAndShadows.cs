using RimWorld;
using SolarWeb.Stratum.MapComponents;
using SolarWeb.Stratum.Stats;
using UnityEngine;
using Verse;

namespace SolarWeb.Stratum.Graphics;

public class SectionLayer_RoofLightingAndShadows : SectionLayer
{
  private static Material? roofOverlayMat;
  private static Material? skylightTintMat;

  private static Material RoofOverlayMat
  {
    get
    {
      if (roofOverlayMat == null)
      {
        roofOverlayMat = new Material(MatBases.LightOverlay.shader);
        roofOverlayMat.CopyPropertiesFromMaterial(MatBases.LightOverlay);
        roofOverlayMat.renderQueue = 3161;
      }
      return roofOverlayMat;
    }
  }

  private static Material SkylightTintMat
  {
    get
    {
      if (skylightTintMat == null)
      {
        skylightTintMat = new Material(MatBases.LightOverlay.shader);
        skylightTintMat.mainTexture = BaseContent.WhiteTex;
        skylightTintMat.renderQueue = 3162;
      }
      return skylightTintMat;
    }
  }

  public SectionLayer_RoofLightingAndShadows(Section section) : base(section)
  {
    relevantChangeTypes = MapMeshFlagDefOf.Roofs | MapMeshFlagDefOf.Buildings;
  }

  public override bool Visible => true;

  public override void DrawLayer()
  {
    if (roofOverlayMat != null)
    {
      roofOverlayMat.color = MatBases.LightOverlay.color;
    }
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

    RoofGrid roofGrid = map.roofGrid;
    LayerSubMesh tintMesh = GetSubMesh(SkylightTintMat);
    float y = AltitudeLayer.LightingOverlay.AltitudeFor() + 0.002f;
    CellRect rect = section.CellRect;

    for (int z = rect.minZ; z <= rect.maxZ; z++)
    {
      for (int x = rect.minX; x <= rect.maxX; x++)
      {
        Color32 cBL = GetTintVertexColor(map, roofGrid, x, z);
        Color32 cBR = GetTintVertexColor(map, roofGrid, x + 1, z);
        Color32 cTL = GetTintVertexColor(map, roofGrid, x, z + 1);
        Color32 cTR = GetTintVertexColor(map, roofGrid, x + 1, z + 1);

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

  private static bool HasTint(Color32 c) => c.r > 0 || c.g > 0 || c.b > 0;

  private static Color32 GetTintVertexColor(Map map, RoofGrid roofGrid, int vx, int vz)
  {
    float sumR = 0f, sumG = 0f, sumB = 0f;
    int count = 0;

    for (int dx = -1; dx <= 0; dx++)
    {
      for (int dz = -1; dz <= 0; dz++)
      {
        IntVec3 c = new(vx + dx, 0, vz + dz);
        if (!c.InBounds(map)) continue;

        RoofDef roof = roofGrid.RoofAt(c);
        if (roof == null || !RoofStatCache.IsSkylight(roof)) continue;

        Color tint = RoofStatCache.GetGlassTint(roof, map, c);
        if (tint == Color.white) continue;

        float trans = RoofStatCache.GetEffectiveTransparency(roof, map, c);
        if (trans <= 0f) continue;

        float strength = trans * 0.70f;
        sumR += tint.r * 255f * strength;
        sumG += tint.g * 255f * strength;
        sumB += tint.b * 255f * strength;
        count++;
      }
    }

    if (count == 0) return new Color32(0, 0, 0, 0);

    return new Color32(
      (byte)Mathf.Clamp(sumR / count, 0f, 255f),
      (byte)Mathf.Clamp(sumG / count, 0f, 255f),
      (byte)Mathf.Clamp(sumB / count, 0f, 255f),
      0
    );
  }

  public static byte GetSkylightCornerShadowAlpha(Map map, RoofGrid roofGrid, int vx, int vz, byte baseA)
  {
    for (int dx = -1; dx <= 0; dx++)
    {
      for (int dz = -1; dz <= 0; dz++)
      {
        if (IsOpaqueRoof(map, roofGrid, vx + dx, vz + dz))
        {
          return (byte)Mathf.Max(baseA, 80);
        }
      }
    }
    return baseA;
  }

  public static bool IsOpaqueRoof(Map map, RoofGrid roofGrid, int x, int z)
  {
    IntVec3 c = new(x, 0, z);
    if (!c.InBounds(map)) return false;
    RoofDef roof = roofGrid.RoofAt(c);
    if (roof == null) return false;
    return !RoofStatCache.IsSkylight(roof) || RoofStatCache.GetEffectiveTransparency(roof, map, c) <= 0f;
  }

  private static void AppendQuad(LayerSubMesh sm, float minX, float minZ, float maxX, float maxZ, float y,
                                 byte aBL, byte aBR, byte aTL, byte aTR)
  {
    AppendQuadCorners(sm, minX, minZ, maxX, maxZ, y, aBL, aBR, aTL, aTR);
  }

  private static void AppendQuadHorizontalGrad(LayerSubMesh sm, float minX, float minZ, float maxX, float maxZ, float y,
                                               byte aLeft, byte aRight)
  {
    AppendQuadCorners(sm, minX, minZ, maxX, maxZ, y, aLeft, aRight, aLeft, aRight);
  }

  private static void AppendQuadCorners(LayerSubMesh sm, float minX, float minZ, float maxX, float maxZ, float y,
                                        byte aBL, byte aBR, byte aTL, byte aTR)
  {
    int i = sm.verts.Count;
    sm.verts.Add(new Vector3(minX, y, minZ)); // botLeft
    sm.verts.Add(new Vector3(minX, y, maxZ)); // topLeft
    sm.verts.Add(new Vector3(maxX, y, maxZ)); // topRight
    sm.verts.Add(new Vector3(maxX, y, minZ)); // botRight

    sm.colors.Add(new Color32(0, 0, 0, aBL));
    sm.colors.Add(new Color32(0, 0, 0, aTL));
    sm.colors.Add(new Color32(0, 0, 0, aTR));
    sm.colors.Add(new Color32(0, 0, 0, aBR));

    sm.tris.Add(i);
    sm.tris.Add(i + 1);
    sm.tris.Add(i + 2);
    sm.tris.Add(i);
    sm.tris.Add(i + 2);
    sm.tris.Add(i + 3);
  }

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
