using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

using SolarWeb.Stratum.UI;

namespace SolarWeb.Stratum.Patches;

[HarmonyPatch]
[StaticConstructorOnStartup]
public static class SelectionDrawer_Patch
{
  private static readonly Material SelectionBracketMat = MaterialPool.MatFrom("UI/Overlays/SelectionBracket", ShaderDatabase.MetaOverlay, Color.white, 4650);
  private static readonly Vector3[] bracketLocs = new Vector3[4];

  [HarmonyPatch(typeof(SelectionDrawer), "DrawSelectionBracketFor")]
  [HarmonyPostfix]
  public static void DrawSelectionBracketFor_Postfix(object obj)
  {
    if (obj is SelectedRoof sr)
    {
      Vector3 center = sr.cell.ToVector3Shifted();
      SelectionDrawerUtility.CalculateSelectionBracketPositionsWorld(bracketLocs, sr, center, Vector2.one, SelectionDrawer.SelectTimes, Vector2.one);
      float highY = AltitudeLayer.MetaOverlays.AltitudeFor() + 0.1f;
      for (int i = 0; i < 4; i++)
      {
        bracketLocs[i].y = highY;
      }

      int angle = 0;
      for (int i = 0; i < 4; i++)
      {
        Quaternion q = Quaternion.AngleAxis(angle, Vector3.up);
        UnityEngine.Graphics.DrawMesh(MeshPool.plane10, Matrix4x4.TRS(bracketLocs[i], q, Vector3.one), SelectionBracketMat, 0);
        angle -= 90;
      }
    }
  }
}
