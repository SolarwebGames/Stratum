using HarmonyLib;
using Verse;
using SolarWeb.Stratum.DefModExtensions;

namespace SolarWeb.Stratum.Patches;

[HarmonyPatch]
public static class DesignationCategoryDef_Patch
{
  [HarmonyPatch(typeof(DesignationCategoryDef), "ResolveDesignators")]
  [HarmonyPostfix]
  public static void ResolveDesignators_Postfix(DesignationCategoryDef __instance)
  {
    foreach (var kvp in BuildableRoofGenerator.RoofToDesignator)
    {
      var roofDef = kvp.Key;
      var designator = kvp.Value;
      var extension = roofDef.GetModExtension<BuildableRoofExtension>();

      if (extension != null && extension.designationCategory == __instance)
      {
        if (!__instance.AllResolvedDesignators.Contains(designator))
        {
          __instance.AllResolvedDesignators.Add(designator);
        }
      }
    }
  }
}
