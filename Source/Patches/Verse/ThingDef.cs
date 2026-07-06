using HarmonyLib;
using Verse;

using SolarWeb.Stratum.UI;
using SolarWeb.Stratum.MapComponents;
using SolarWeb.Stratum.DefModExtensions;

namespace SolarWeb.Stratum.Patches;

[HarmonyPatch(typeof(ThingDef))]
public static class ThingDef_Patch
{
  [HarmonyPatch(nameof(ThingDef.DescriptionDetailed), MethodType.Getter)]
  [HarmonyPostfix]
  public static void DescriptionDetailed_Postfix(ThingDef __instance, ref string __result)
  {
    if (__instance.defName != null && __instance.defName.Contains("SolarWeb-Stratum-Roof"))
    {
      var selectedRoof = Find.Selector.SingleSelectedObject as SelectedRoof;
      if (selectedRoof != null && selectedRoof.def != null)
      {
        var ext = selectedRoof.def.GetModExtension<BuildableRoofExtension>();
        if (ext != null && ext.buildableDef == __instance)
        {
          var stuff = selectedRoof.map.GetComponent<RoofIntegrityGrid>()?.GetStuff(selectedRoof.cell);
          if (stuff != null)
          {
            string adjective = stuff.stuffProps?.stuffAdjective ?? stuff.label;
            __result = __result.Replace("stone", adjective).Replace("various materials", adjective).Replace("materials", adjective);
          }
        }
      }
    }
  }
}
