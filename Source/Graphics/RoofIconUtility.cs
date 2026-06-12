using UnityEngine;
using Verse;

using SolarWeb.Stratum.Stats;
using SolarWeb.Stratum.DefModExtensions;

namespace SolarWeb.Stratum.Graphics;

public static class RoofIconUtility
{
  public static void TryExtractIcon(RoofDef roofDef, BuildableRoofExtension ext, ref Texture icon, ref Rect iconTexCoords)
  {
    var gd = RoofStatCache.GetGraphicData(roofDef);
    var bDef = ext.buildableDef;

    if ((icon == null || icon == BaseContent.BadTex || bDef?.uiIcon == icon) && gd != null && RoofAtlasManager.TryGetUv(gd.texPath, out var uvs, out var mat))
    {
      if (uvs != null && uvs.Length >= 4)
      {
        float minU = uvs[0].x;
        float minV = uvs[0].y;
        float maxU = uvs[2].x;
        float maxV = uvs[2].y;

        RenderTexture rt = RenderTexture.GetTemporary(64, 64, 0, RenderTextureFormat.Default, RenderTextureReadWrite.Linear);
        Vector2 scale = new Vector2(maxU - minU, maxV - minV);
        Vector2 offset = new Vector2(minU, minV);
        UnityEngine.Graphics.Blit(mat.mainTexture, rt, scale, offset);

        RenderTexture prev = RenderTexture.active;
        RenderTexture.active = rt;
        Texture2D resolvedIcon = new Texture2D(64, 64, TextureFormat.RGBA32, false);
        resolvedIcon.ReadPixels(new Rect(0, 0, 64, 64), 0, 0);
        resolvedIcon.Apply();
        RenderTexture.active = prev;
        RenderTexture.ReleaseTemporary(rt);

        icon = resolvedIcon;
        iconTexCoords = new Rect(0, 0, 1, 1);

        if (bDef != null) bDef.uiIcon = resolvedIcon;
      }
      else
      {
        icon = mat.mainTexture;
      }
    }
  }

  public static void DrawDesignatorIcon(Rect rect, RoofDef roofDef, ThingDef? stuffDef, Color? selectedTint, Color defaultIconColor, Texture icon, Rect iconTexCoords, Material? buttonMat, GizmoRenderParms parms)
  {
    if (icon == null) return;

    var gd = RoofStatCache.GetGraphicData(roofDef);

    if (gd != null && RoofStatCache.IsSkylight(roofDef) && gd.skylightFrameWidth > 0f)
    {
      float f = gd.skylightFrameWidth;
      float glassAlpha = 1f - RoofStatCache.GetTransparency(roofDef);

      var ext = roofDef.GetModExtension<BuildableRoofExtension>();
      Color frameColor = (stuffDef != null && ext?.buildableDef != null)
          ? ext.buildableDef.GetColorForStuff(stuffDef)
          : defaultIconColor;
      frameColor.a = 1f;

      Color glassColor = selectedTint ?? RoofStatCache.GetGlassTint(roofDef);
      glassColor.a = glassAlpha;

      if (parms.lowLight)
      {
        frameColor.a *= 0.6f;
        glassColor.a *= 0.6f;
      }

      Rect tc = iconTexCoords;
      float[] rx = { rect.xMin, rect.xMin + rect.width * f, rect.xMin + rect.width * (1f - f), rect.xMax };
      float[] ry = { rect.yMin, rect.yMin + rect.height * f, rect.yMin + rect.height * (1f - f), rect.yMax };

      float[] ux = { tc.xMin, tc.xMin + tc.width * f, tc.xMin + tc.width * (1f - f), tc.xMax };
      float[] uy = { tc.yMin, tc.yMin + tc.height * f, tc.yMin + tc.height * (1f - f), tc.yMax };

      for (int x = 0; x < 3; x++)
      {
        for (int y = 0; y < 3; y++)
        {
          bool isCenter = (x == 1 && y == 1);
          GUI.color = isCenter ? glassColor : frameColor;

          Rect qRect = new Rect(rx[x], ry[y], rx[x + 1] - rx[x], ry[y + 1] - ry[y]);
          Rect qUv = new Rect(ux[x], uy[2 - y], ux[x + 1] - ux[x], uy[2 - y + 1] - uy[2 - y]);

          GUI.DrawTextureWithTexCoords(qRect, (Texture2D)icon, qUv);
        }
      }
      GUI.color = Color.white;
    }
    else
    {
      Color iconColor;
      if (RoofStatCache.IsSkylight(roofDef))
      {
        iconColor = selectedTint ?? RoofStatCache.GetGlassTint(roofDef);
      }
      else if (stuffDef != null)
      {
        var bDef = roofDef.GetModExtension<BuildableRoofExtension>()?.buildableDef;
        iconColor = (bDef != null) ? bDef.GetColorForStuff(stuffDef) : defaultIconColor;
      }
      else
      {
        iconColor = defaultIconColor;
      }

      GUI.color = iconColor;
      if (parms.lowLight) GUI.color = GUI.color.ToTransparent(0.6f);

      Widgets.DrawTextureFitted(rect, icon, 0.85f, Vector2.one, iconTexCoords, 0f, buttonMat);
      GUI.color = Color.white;
    }
  }
}
