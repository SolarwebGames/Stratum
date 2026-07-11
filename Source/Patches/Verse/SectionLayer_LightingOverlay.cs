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
  internal struct CellGlowData
  {
    public Color32 visualGlow;
    public bool flag;
    public bool blockLight;
    public bool forcesCenterAlpha100;
  }

  [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
  private static void AddNeighborGlow(
    in CellGlowData data,
    ref int rSum, ref int gSum, ref int bSum, ref int aSum,
    ref int num5, ref bool flag)
  {
    if (data.flag) flag = true;
    if (!data.blockLight)
    {
      rSum += data.visualGlow.r;
      gSum += data.visualGlow.g;
      bSum += data.visualGlow.b;
      aSum += data.visualGlow.a;
      num5++;
    }
  }

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
    int z = map.Size.z;
    Thing[] innerArray = map.edificeGrid.InnerArray;
    RoofGrid roofGrid = map.roofGrid;
    CellIndices cellIndices = map.cellIndices;

    CalculateVertexIndices(rect, firstCenterInd, rect.minX, rect.minZ, out var botLeft, out var _, out var topRight, out var botRight, out var center);
    int num2 = cellIndices.CellToIndex(new IntVec3(rect.minX, 0, rect.minZ));

    var integrity = map.GetComponent<RoofIntegrityGrid>();
    Color?[]? glassTints = integrity?.GlassTintsArray;
    float skyGlow = map.skyManager?.CurSkyGlow ?? 0f;
    float skyGlow255 = skyGlow * 255f;
    bool lowGlow = skyGlow <= 0.01f;
    bool hasFilter = filter != null;

    var cacheComp = map.GetComponent<LightingOverlayCache>();
    int currentFrame = Time.frameCount;

    int cacheMinX = rect.minX - 1;
    int cacheMinZ = rect.minZ - 1;
    int cacheMaxX = maxX + 1;
    int cacheMaxZ = maxZ + 1;

    int cacheWidth = cacheMaxX - cacheMinX + 1;
    int cacheHeight = cacheMaxZ - cacheMinZ + 1;
    int cacheSize = cacheWidth * cacheHeight;

    Span<CellGlowData> cache = cacheSize <= 512
      ? stackalloc CellGlowData[cacheSize]
      : new CellGlowData[cacheSize];

    int startZ = Math.Max(0, cacheMinZ);
    int endZ = Math.Min(z - 1, cacheMaxZ);
    int startX = Math.Max(0, cacheMinX);
    int endX = Math.Min(x - 1, cacheMaxX);

    for (int zIndex = startZ; zIndex <= endZ; zIndex++)
    {
      int rowOffset = (zIndex - cacheMinZ) * cacheWidth;
      int cellIndexStart = zIndex * x;

      for (int xIndex = startX; xIndex <= endX; xIndex++)
      {
        int cacheIdx = rowOffset + (xIndex - cacheMinX);
        int mapIdx = cellIndexStart + xIndex;

        if (cacheComp != null && cacheComp.lastCellUpdateFrame[mapIdx] == currentFrame)
        {
          cache[cacheIdx] = cacheComp.cachedGlowData[mapIdx];
          continue;
        }

        Thing thing = innerArray[mapIdx];
        RoofDef roofDef = roofGrid.RoofAt(mapIdx);

        bool isSkylight = roofDef != null && RoofStatCache.isSkylightByIndex[roofDef.index];
        bool flag = false;
        if (roofDef != null && !isSkylight && (roofDef.isThickRoof || thing == null || !thing.def.holdsRoof || thing.def.altitudeLayer == AltitudeLayer.DoorMoveable))
        {
          flag = true;
        }

        bool blockLight = thing != null && thing.def.blockLight;
        Color32 visualGlow = default;

        if (!blockLight)
        {
          visualGlow = map.glowGrid.VisualGlowAt(mapIdx);

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
                Color glassColor = Color.white;
                if (glassTints != null)
                {
                  var tint = glassTints[mapIdx];
                  if (tint.HasValue) glassColor = tint.Value;
                  else glassColor = RoofStatCache.glassTintByIndex[roofDef.index];
                }
                else
                {
                  glassColor = RoofStatCache.glassTintByIndex[roofDef.index];
                }

                float transparencySkyGlow = transparency * skyGlow255;
                int rVal = visualGlow.r + (int)(glassColor.r * transparencySkyGlow);
                int gVal = visualGlow.g + (int)(glassColor.g * transparencySkyGlow);
                int bVal = visualGlow.b + (int)(glassColor.b * transparencySkyGlow);

                int maxColor = rVal > gVal ? rVal : gVal;
                maxColor = maxColor > bVal ? maxColor : bVal;
                int finalA = maxColor > 100 ? maxColor : 100;

                if (finalA > 255)
                {
                  visualGlow.r = (byte)(rVal * 255 / finalA);
                  visualGlow.g = (byte)(gVal * 255 / finalA);
                  visualGlow.b = (byte)(bVal * 255 / finalA);
                  visualGlow.a = 255;
                }
                else
                {
                  visualGlow.r = (byte)rVal;
                  visualGlow.g = (byte)gVal;
                  visualGlow.b = (byte)bVal;
                  visualGlow.a = (byte)finalA;
                }
              }
            }
          }
        }

        CellGlowData data = new CellGlowData
        {
          visualGlow = visualGlow,
          flag = flag,
          blockLight = blockLight,
          forcesCenterAlpha100 = roofDef != null && !isSkylight && (thing == null || !thing.def.holdsRoof)
        };

        cache[cacheIdx] = data;
        if (cacheComp != null)
        {
          cacheComp.cachedGlowData[mapIdx] = data;
          cacheComp.lastCellUpdateFrame[mapIdx] = currentFrame;
        }
      }
    }

    for (int i = rect.minZ; i <= maxZ + 1; i++)
    {
      int num4 = rect.minX;
      bool validZBottom = i > 0;
      bool validZTop = i < z;

      int cacheZBottomOffset = (i - 1 - cacheMinZ) * cacheWidth;
      int cacheZTopOffset = (i - cacheMinZ) * cacheWidth;

      while (num4 <= maxX + 1)
      {
        if (hasFilter && !filter!(num2))
        {
          array[botLeft] = default;
        }
        else
        {
          int rSum = 0;
          int gSum = 0;
          int bSum = 0;
          int aSum = 0;
          int num5 = 0;
          bool flag = false;

          bool validXLeft = num4 > 0;
          bool validXRight = num4 < x;

          int cacheXLeft = num4 - 1 - cacheMinX;
          int cacheXRight = num4 - cacheMinX;

          if (validXLeft && validZBottom)
          {
            AddNeighborGlow(cache[cacheZBottomOffset + cacheXLeft], ref rSum, ref gSum, ref bSum, ref aSum, ref num5, ref flag);
          }

          if (validXRight && validZBottom)
          {
            AddNeighborGlow(cache[cacheZBottomOffset + cacheXRight], ref rSum, ref gSum, ref bSum, ref aSum, ref num5, ref flag);
          }

          if (validXLeft && validZTop)
          {
            AddNeighborGlow(cache[cacheZTopOffset + cacheXLeft], ref rSum, ref gSum, ref bSum, ref aSum, ref num5, ref flag);
          }

          if (validXRight && validZTop)
          {
            AddNeighborGlow(cache[cacheZTopOffset + cacheXRight], ref rSum, ref gSum, ref bSum, ref aSum, ref num5, ref flag);
          }

          Color32 finalColor = default;
          if (num5 > 0)
          {
            switch (num5)
            {
              case 4:
                finalColor = new Color32(
                  (byte)(rSum >> 2),
                  (byte)(gSum >> 2),
                  (byte)(bSum >> 2),
                  (byte)(aSum >> 2)
                );
                break;
              case 3:
                finalColor = new Color32(
                  (byte)(rSum / 3),
                  (byte)(gSum / 3),
                  (byte)(bSum / 3),
                  (byte)(aSum / 3)
                );
                break;
              case 2:
                finalColor = new Color32(
                  (byte)(rSum >> 1),
                  (byte)(gSum >> 1),
                  (byte)(bSum >> 1),
                  (byte)(aSum >> 1)
                );
                break;
              case 1:
                finalColor = new Color32(
                  (byte)rSum,
                  (byte)gSum,
                  (byte)bSum,
                  (byte)aSum
                );
                break;
            }
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
      int cacheZOffset = (k - cacheMinZ) * cacheWidth;

      while (num9 <= maxX)
      {
        if (hasFilter && !filter!(num8))
        {
          array[center2] = default;
        }
        else
        {
          int r = array[botLeft2].r + array[botLeft2 + 1].r + array[botLeft2 + width + 1].r + array[botLeft2 + width + 2].r;
          int g = array[botLeft2].g + array[botLeft2 + 1].g + array[botLeft2 + width + 1].g + array[botLeft2 + width + 2].g;
          int b = array[botLeft2].b + array[botLeft2 + 1].b + array[botLeft2 + width + 1].b + array[botLeft2 + width + 2].b;
          int a = array[botLeft2].a + array[botLeft2 + 1].a + array[botLeft2 + width + 1].a + array[botLeft2 + width + 2].a;

          Color32 centerFinal = new((byte)(r >> 2), (byte)(g >> 2), (byte)(b >> 2), (byte)(a >> 2));

          int cacheIdx = cacheZOffset + (num9 - cacheMinX);
          if (centerFinal.a < 100 && cache[cacheIdx].forcesCenterAlpha100)
          {
            centerFinal.a = 100;
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
