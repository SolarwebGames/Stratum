using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;
using SolarWeb.Stratum.UI;
using SolarWeb.Stratum.MapComponents;
using SolarWeb.Stratum.Stats;
using System;

namespace SolarWeb.Stratum.Patches;

[HarmonyPatch(typeof(InspectPaneFiller))]
[StaticConstructorOnStartup]
public static class InspectPaneFiller_Patch
{
  private static Texture2D? barBGTex;
  private static Texture2D BarBGTex => barBGTex ??= (Texture2D)AccessTools.Field(typeof(InspectPaneFiller), "BarBGTex").GetValue(null);

  private static Texture2D? healthTex;
  private static Texture2D HealthTex => healthTex ??= (Texture2D)AccessTools.Field(typeof(InspectPaneFiller), "HealthTex").GetValue(null);

  [HarmonyPatch("DoPaneContentsFor")]
  [HarmonyPrefix]
  public static bool DoPaneContentsFor_Prefix(ISelectable sel, Rect rect)
  {
    if (sel is SelectedRoof sr)
    {
      try
      {
        Widgets.BeginGroup(rect);
        float num = 0f;
        num += 3f;
        WidgetRow row = new WidgetRow(0f, num);
        DrawHealth(row, sr);
        num += 18f;

        Rect rect2 = rect.AtZero();
        rect2.yMin = num;
        InspectPaneFiller.DrawInspectStringFor(sel, rect2);
      }
      catch (Exception arg)
      {
        StratumLog.Error($"Error in DoPaneContentsFor SelectedRoof {sel}: {arg}");
      }
      finally
      {
        Widgets.EndGroup();
      }
      return false;
    }
    return true;
  }

  private static void DrawHealth(WidgetRow row, SelectedRoof sr)
  {
    var integrity = sr.map.GetComponent<RoofIntegrityGrid>();
    if (integrity == null) return;

    int hp = integrity.GetHitPoints(sr.cell);
    int maxHp = integrity.GetMaxHitPoints(sr.cell);
    if (maxHp <= 0) return;

    float fillPct = (float)hp / maxHp;
    string label = hp.ToStringCached() + " / " + maxHp.ToStringCached();

    if (hp >= maxHp) GUI.color = Color.white;
    else if ((float)hp > (float)maxHp * 0.5f) GUI.color = Color.yellow;
    else if (hp > 0) GUI.color = Color.red;
    else GUI.color = Color.grey;

    row.FillableBar(93f, 16f, fillPct, label, HealthTex, BarBGTex);
    GUI.color = Color.white;
  }
}
