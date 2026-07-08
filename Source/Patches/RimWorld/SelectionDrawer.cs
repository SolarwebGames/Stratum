using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

using SolarWeb.Stratum.UI;
using SolarWeb.Stratum.WorldComponents;

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

      float selectTime = RoofSelectionTracker.Instance.GetSelectTimeFor(sr);

      float num = Mathf.Max(0f, 1f - (Time.realtimeSinceStartup - selectTime) / 0.07f);
      float offset = num * 0.2f;
      float highY = AltitudeLayer.MetaOverlays.AltitudeFor() + 0.1f;

      bracketLocs[0] = new Vector3(center.x - offset, highY, center.z - offset);
      bracketLocs[1] = new Vector3(center.x + offset, highY, center.z - offset);
      bracketLocs[2] = new Vector3(center.x + offset, highY, center.z + offset);
      bracketLocs[3] = new Vector3(center.x - offset, highY, center.z + offset);

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

