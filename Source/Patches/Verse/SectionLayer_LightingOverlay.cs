using System;
using HarmonyLib;
using UnityEngine;
using Verse;

using SolarWeb.Stratum.Stats;
using SolarWeb.Stratum.MapComponents;

namespace SolarWeb.Stratum.Patches;

[HarmonyPatch(typeof(SectionLayer_LightingOverlay))]
public static class SectionLayer_LightingOverlay_Patch
{
  [HarmonyPatch("GenerateLightingOverlay")]
  [HarmonyPrefix]
  public static bool GenerateLightingOverlay_Prefix(Map map, LayerSubMesh subMesh, CellRect rect, ref int firstCenterInd, bool centered = false, Predicate<int>? filter = null)
  {
    rect.ClipInsideMap(map);
    if (subMesh.verts.Count == 0)
    {
      MakeBaseGeometry(map, subMesh, rect, out firstCenterInd, centered);
    }

    Color32[] array = new Color32[subMesh.verts.Count];
    int maxX = rect.maxX;
    int maxZ = rect.maxZ;
    int width = rect.Width;
    int x = map.Size.x;
    int z = map.Size.z; // THE FIX: Track strict map height
    Thing[] innerArray = map.edificeGrid.InnerArray;
    RoofGrid roofGrid = map.roofGrid;
    CellIndices cellIndices = map.cellIndices;

    CalculateVertexIndices(rect, firstCenterInd, rect.minX, rect.minZ, out var botLeft, out var _, out var topRight, out var botRight, out var center);
    int num2 = cellIndices.CellToIndex(new IntVec3(rect.minX, 0, rect.minZ));

    var integrity = map.GetComponent<RoofIntegrityGrid>();
    float skyGlow = map.skyManager?.CurSkyGlow ?? 0f;
    float skyGlow255 = skyGlow * 255f;
    bool lowGlow = skyGlow <= 0.01f;
    bool hasFilter = filter != null;

    for (int i = rect.minZ; i <= maxZ + 1; i++)
    {
      int num4 = rect.minX;

      // THE FIX: Cache Z bounds outside the inner loop
      bool validZBottom = i > 0;
      bool validZTop = i < z;

      while (num4 <= maxX + 1)
      {
        if (hasFilter && !filter!(num2))
        {
          array[botLeft] = default;
        }
        else
        {
          ColorInt colorInt = default;
          int num5 = 0;
          bool flag = false;

          // THE FIX: Strict, zero-division Cartesian bounds for the current vertex
          bool validXLeft = num4 > 0;
          bool validXRight = num4 < x;

          // Neighbor 0: Offset (-1, -1)
          if (validXLeft && validZBottom)
          {
            ProcessCell(num2 - x - 1, num4 - 1, i - 1, innerArray, roofGrid, map, integrity, skyGlow255, lowGlow, ref colorInt, ref num5, ref flag);
          }

          // Neighbor 1: Offset (0, -1)
          if (validXRight && validZBottom)
          {
            ProcessCell(num2 - x, num4, i - 1, innerArray, roofGrid, map, integrity, skyGlow255, lowGlow, ref colorInt, ref num5, ref flag);
          }

          // Neighbor 2: Offset (-1, 0)
          if (validXLeft && validZTop)
          {
            ProcessCell(num2 - 1, num4 - 1, i, innerArray, roofGrid, map, integrity, skyGlow255, lowGlow, ref colorInt, ref num5, ref flag);
          }

          // Neighbor 3: Offset (0, 0)
          if (validXRight && validZTop)
          {
            ProcessCell(num2, num4, i, innerArray, roofGrid, map, integrity, skyGlow255, lowGlow, ref colorInt, ref num5, ref flag);
          }

          Color32 finalColor = default;
          if (num5 > 0)
          {
            finalColor = (colorInt / num5).ProjectToColor32();
          }

          if (flag && finalColor.a < 100)
          {
            finalColor.a = 100;
          }

          array[botLeft] = finalColor;
        }

        num4++;
        botLeft++;
        num2++;
      }

      int num7 = maxX + 2 - rect.minX;
      botLeft -= num7;
      num2 -= num7;
      botLeft += width + 1;
      num2 += x;
    }

    CalculateVertexIndices(rect, firstCenterInd, rect.minX, rect.minZ, out var botLeft2, out center, out botRight, out topRight, out var center2);
    int num8 = cellIndices.CellToIndex(rect.minX, rect.minZ);

    for (int k = rect.minZ; k <= maxZ; k++)
    {
      int num9 = rect.minX;
      while (num9 <= maxX)
      {
        if (hasFilter && !filter!(num8))
        {
          array[center2] = default;
        }
        else
        {
          ColorInt colorInt2 = default;
          colorInt2 += array[botLeft2];
          colorInt2 += array[botLeft2 + 1];
          colorInt2 += array[botLeft2 + width + 1];
          colorInt2 += array[botLeft2 + width + 2];

          Color32 centerFinal = new((byte)(colorInt2.r / 4), (byte)(colorInt2.g / 4), (byte)(colorInt2.b / 4), (byte)(colorInt2.a / 4));

          var centerRoof = roofGrid.RoofAt(num8);
          bool centerIsSkylight = centerRoof != null && RoofStatCache.isSkylightByIndex[centerRoof.index];

          if (centerFinal.a < 100 && centerRoof != null && !centerIsSkylight)
          {
            Thing thing2 = innerArray[num8];
            if (thing2 == null || !thing2.def.holdsRoof)
            {
              centerFinal.a = 100;
            }
          }

          array[center2] = centerFinal;
        }
        num9++;
        botLeft2++;
        center2++;
        num8++;
      }
      botLeft2++;
      num8 -= width;
      num8 += x;
    }

    subMesh.mesh.colors32 = array;
    return false;
  }

  [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
  private static void ProcessCell(
      int index, int targetX, int targetZ,
      Thing[] innerArray, RoofGrid roofGrid, Map map, RoofIntegrityGrid integrity,
      float skyGlow255, bool lowGlow,
      ref ColorInt colorInt, ref int num5, ref bool flag)
  {
    Thing thing = innerArray[index];
    RoofDef roofDef = roofGrid.RoofAt(index);

    bool isSkylight = roofDef != null && RoofStatCache.isSkylightByIndex[roofDef.index];
    if (roofDef != null && !isSkylight && (roofDef.isThickRoof || thing == null || !thing.def.holdsRoof || thing.def.altitudeLayer == AltitudeLayer.DoorMoveable))
    {
      flag = true;
    }

    if (thing == null || !thing.def.blockLight)
    {
      Color32 visualGlow = map.glowGrid.VisualGlowAt(index);

      if (isSkylight)
      {
        float transparency = RoofStatCache.transparencyByIndex[roofDef!.index];
        if (transparency > 0f)
        {
          if (lowGlow)
          {
            if (visualGlow.a < 100) visualGlow.a = 100;
          }
          else
          {
            IntVec3 cell = new(targetX, 0, targetZ);
            Color glassColor = RoofStatCache.GetGlassTint(roofDef, integrity, cell);

            float transparencySkyGlow = transparency * skyGlow255;
            int r = visualGlow.r + (int)(glassColor.r * transparencySkyGlow);
            int g = visualGlow.g + (int)(glassColor.g * transparencySkyGlow);
            int b = visualGlow.b + (int)(glassColor.b * transparencySkyGlow);

            int maxColor = r > g ? r : g;
            maxColor = maxColor > b ? maxColor : b;
            int finalA = maxColor > 100 ? maxColor : 100;

            if (finalA > 255)
            {
              visualGlow.r = (byte)(r * 255 / finalA);
              visualGlow.g = (byte)(g * 255 / finalA);
              visualGlow.b = (byte)(b * 255 / finalA);
              visualGlow.a = 255;
            }
            else
            {
              visualGlow.r = (byte)r;
              visualGlow.g = (byte)g;
              visualGlow.b = (byte)b;
              visualGlow.a = (byte)finalA;
            }
          }
        }
      }

      colorInt += visualGlow;
      num5++;
    }
  }

  private static void MakeBaseGeometry(Map map, LayerSubMesh sm, CellRect sectRect, out int firstCenterInd, bool centered = false)
  {
    sectRect.ClipInsideMap(map);
    float num = (centered ? ((float)(-sectRect.minX) - (float)sectRect.Width / 2f) : 0f);
    float num2 = (centered ? ((float)(-sectRect.minZ) - (float)sectRect.Height / 2f) : 0f);
    int capacity = (sectRect.Width + 1) * (sectRect.Height + 1) + sectRect.Area;
    float y = AltitudeLayer.LightingOverlay.AltitudeFor();
    sm.verts.Capacity = capacity;
    for (int i = sectRect.minZ; i <= sectRect.maxZ + 1; i++)
    {
      for (int j = sectRect.minX; j <= sectRect.maxX + 1; j++)
      {
        sm.verts.Add(new Vector3((float)j + num, y, (float)i + num2));
      }
    }
    firstCenterInd = sm.verts.Count;
    for (int k = sectRect.minZ; k <= sectRect.maxZ; k++)
    {
      for (int l = sectRect.minX; l <= sectRect.maxX; l++)
      {
        sm.verts.Add(new Vector3((float)l + num + 0.5f, y, (float)k + num2 + 0.5f));
      }
    }

    sm.tris.Capacity = sectRect.Area * 4 * 3;
    for (int m = sectRect.minZ; m <= sectRect.maxZ; m++)
    {
      for (int n = sectRect.minX; n <= sectRect.maxX; n++)
      {
        CalculateVertexIndices(sectRect, firstCenterInd, n, m, out var botLeft, out var topLeft, out var topRight, out var botRight, out var center);
        sm.tris.Add(botLeft);
        sm.tris.Add(center);
        sm.tris.Add(botRight);
        sm.tris.Add(botLeft);
        sm.tris.Add(topLeft);
        sm.tris.Add(center);
        sm.tris.Add(topLeft);
        sm.tris.Add(topRight);
        sm.tris.Add(center);
        sm.tris.Add(topRight);
        sm.tris.Add(botRight);
        sm.tris.Add(center);
      }
    }
    sm.FinalizeMesh(MeshParts.Verts | MeshParts.Tris);
  }

  private static void CalculateVertexIndices(CellRect sectRect, int firstCenterInd, int worldX, int worldZ, out int botLeft, out int topLeft, out int topRight, out int botRight, out int center)
  {
    int num = worldX - sectRect.minX;
    int num2 = worldZ - sectRect.minZ;
    botLeft = num2 * (sectRect.Width + 1) + num;
    topLeft = (num2 + 1) * (sectRect.Width + 1) + num;
    topRight = (num2 + 1) * (sectRect.Width + 1) + (num + 1);
    botRight = num2 * (sectRect.Width + 1) + (num + 1);
    center = firstCenterInd + (num2 * sectRect.Width + num);
  }
}
