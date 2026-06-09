using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using HarmonyLib;
using Verse;
using SolarWeb.Stratum.Stats;

namespace SolarWeb.Stratum.Patches.Transpilers;

[HarmonyPatch]
public static class SectionLayer_LightingOverlay_Patch
{
  [HarmonyPatch(typeof(SectionLayer_LightingOverlay), "GenerateLightingOverlay")]
  [HarmonyTranspiler]
  public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
  {
    var codes = new List<CodeInstruction>(instructions);
    var roofAtMethod = AccessTools.Method(typeof(RoofGrid), nameof(RoofGrid.RoofAt), [typeof(int)]);
    var roofedMethod = AccessTools.Method(typeof(RoofGrid), nameof(RoofGrid.Roofed), [typeof(int)]);
    
    var roofAtReplacement = AccessTools.Method(typeof(SectionLayer_LightingOverlay_Patch), nameof(RoofAtTransparent));
    var roofedReplacement = AccessTools.Method(typeof(SectionLayer_LightingOverlay_Patch), nameof(RoofedTransparent));

    for (int i = 0; i < codes.Count; i++)
    {
      if (codes[i].Calls(roofAtMethod))
      {
        codes[i].opcode = OpCodes.Call;
        codes[i].operand = roofAtReplacement;
      }
      else if (codes[i].Calls(roofedMethod))
      {
        codes[i].opcode = OpCodes.Call;
        codes[i].operand = roofedReplacement;
      }
    }

    return codes.AsEnumerable();
  }

  public static RoofDef? RoofAtTransparent(RoofGrid grid, int index)
  {
    var roof = grid.RoofAt(index);
    if (roof != null && RoofStatCache.IsCustomRoof(roof) && RoofStatCache.GetTransparency(roof) > 0f)
    {
      return null; // Hide from shadow logic if transparent
    }
    return roof;
  }

  public static bool RoofedTransparent(RoofGrid grid, int index)
  {
    var roof = grid.RoofAt(index);
    if (roof != null && RoofStatCache.IsCustomRoof(roof) && RoofStatCache.GetTransparency(roof) > 0f)
    {
      return false; // Not "roofed" for shadow purposes if transparent
    }
    return grid.Roofed(index);
  }
}
