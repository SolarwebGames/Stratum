using HarmonyLib;
using RimWorld;
using Verse;
using UnityEngine;

using SolarWeb.Stratum.UI;
using SolarWeb.Stratum.DefModExtensions;
using SolarWeb.Stratum.MapComponents;

namespace SolarWeb.Stratum.Patches;

[HarmonyPatch]
public static class MainTabWindow_Inspect_Patch
{
  [HarmonyPatch(typeof(MainTabWindow_Inspect), "DoInspectPaneButtons")]
  [HarmonyPostfix]
  public static void DoInspectPaneButtons_Postfix(MainTabWindow_Inspect __instance, Rect rect, ref float lineEndWidth)
  {
    if (Find.Selector.NumSelected == 1 && Find.Selector.SingleSelectedObject is SelectedRoof sr)
    {
      var ext = sr.def.GetModExtension<BuildableRoofExtension>();
      if (ext?.buildableDef != null)
      {
        float x = rect.width - 48f;
        if (__instance.ShouldShowSelectNextInCellButton) x -= 24f;

        var stuff = sr.map.GetComponent<RoofIntegrityGrid>()?.GetStuff(sr.cell);
        Widgets.InfoCardButton(x, 0f, ext.buildableDef, stuff);
        lineEndWidth += 24f;
      }
    }
  }
}
