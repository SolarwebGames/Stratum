using HarmonyLib;
using SolarWeb.Stratum.Stats;
using UnityEngine;
using Verse;

namespace SolarWeb.Stratum.Patches.Transpilers;

[HarmonyPatch]
public static class SectionLayer_SunShadows_Regenerate_Patch
{
  [HarmonyPatch("SectionLayer_SunShadows", "Regenerate")]
  [HarmonyPostfix]
  public static void Regenerate_Postfix(SectionLayer __instance, Section ___section)
  {
    if (___section == null || ___section.map == null) return;
    Map map = ___section.map;
    RoofGrid roofGrid = map.roofGrid;
    if (roofGrid == null) return;
    CellRect cellRect = ___section.CellRect;

    LayerSubMesh subMesh = __instance.GetSubMesh(MatBases.SunShadow);
    float y = AltitudeLayer.Shadows.AltitudeFor();

    float frameHeight = 0.5f;
    Color32 shadowColor = new(0, 0, 0, (byte)(255f * frameHeight));
    Color32 zeroColor = new(0, 0, 0, 0);

    for (int i = cellRect.minX; i <= cellRect.maxX; i++)
    {
      for (int j = cellRect.minZ; j <= cellRect.maxZ; j++)
      {
        int idx = map.cellIndices.CellToIndex(i, j);
        var roof = roofGrid.RoofAt(idx);
        if (roof == null || !RoofStatCache.IsCustomRoof(roof)) continue;

        float trans = RoofStatCache.GetTransparency(roof);
        if (trans <= 0f) continue;

        for (int k = 0; k < 4; k++)
        {
          IntVec3 nc = new IntVec3(i, 0, j) + GenAdj.CardinalDirections[k];
          bool isBorder = false;
          if (!nc.InBounds(map))
          {
            isBorder = true;
          }
          else
          {
            var nRoof = roofGrid.RoofAt(nc);
            if (nRoof != roof) isBorder = true;
          }

          if (isBorder)
          {
            // If there's an edifice that already casts shadows, skip to avoid double shadows
            Building? building = nc.InBounds(map) ? map.edificeGrid[nc] : null;
            if (building != null && building.def.staticSunShadowHeight > 0f) continue;

            Vector3 v1, v2;
            if (k == 0) { v1 = new Vector3(i, y, j + 1); v2 = new Vector3(i + 1, y, j + 1); }
            else if (k == 1) { v1 = new Vector3(i + 1, y, j); v2 = new Vector3(i + 1, y, j + 1); }
            else if (k == 2) { v1 = new Vector3(i, y, j); v2 = new Vector3(i + 1, y, j); }
            else { v1 = new Vector3(i, y, j); v2 = new Vector3(i, y, j + 1); }

            int count = subMesh.verts.Count;
            subMesh.verts.Add(v1);
            subMesh.verts.Add(v2);
            subMesh.colors.Add(zeroColor);
            subMesh.colors.Add(zeroColor);

            subMesh.verts.Add(v1);
            subMesh.verts.Add(v2);
            subMesh.colors.Add(shadowColor);
            subMesh.colors.Add(shadowColor);

            subMesh.tris.Add(count);
            subMesh.tris.Add(count + 2);
            subMesh.tris.Add(count + 3);
            subMesh.tris.Add(count);
            subMesh.tris.Add(count + 3);
            subMesh.tris.Add(count + 1);
          }
        }
      }
    }

    if (subMesh.verts.Count > 0)
    {
      subMesh.finalized = false;
      subMesh.FinalizeMesh(MeshParts.Verts | MeshParts.Tris | MeshParts.Colors);
    }
  }
}

