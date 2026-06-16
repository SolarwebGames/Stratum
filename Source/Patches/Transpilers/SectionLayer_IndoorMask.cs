using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using HarmonyLib;
using SolarWeb.Stratum.Stats;
using Verse;

namespace SolarWeb.Stratum.Patches.Transpilers;

[HarmonyPatch(typeof(SectionLayer_IndoorMask))]
public static class SectionLayer_IndoorMask_Patch
{
  [HarmonyPatch("HideCommon")]
  [HarmonyTranspiler]
  public static IEnumerable<CodeInstruction> HideCommon_Transpiler(IEnumerable<CodeInstruction> instructions)
  {
    var codes = new List<CodeInstruction>(instructions);
    var roofedMethod = AccessTools.Method(typeof(GridsUtility), nameof(GridsUtility.Roofed), [typeof(IntVec3), typeof(Map)]);
    var helperMethod = AccessTools.Method(typeof(SectionLayer_IndoorMask_Patch), nameof(IsRoofedForMask));

    for (int i = 0; i < codes.Count; i++)
    {
      if (codes[i].Calls(roofedMethod))
      {
        codes[i].opcode = OpCodes.Call;
        codes[i].operand = helperMethod;
      }
    }

    return codes.AsEnumerable();
  }

  public static bool IsRoofedForMask(IntVec3 c, Map map)
  {
    var roof = map.roofGrid.RoofAt(c);
    if (roof != null && RoofStatCache.IsCustomRoof(roof) && RoofStatCache.GetTransparency(roof) > 0f)
    {
      return false;
    }
    return map.roofGrid.Roofed(c);
  }
}
