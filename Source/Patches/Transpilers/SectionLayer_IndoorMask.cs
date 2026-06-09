using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using SolarWeb.Stratum.Stats;
using Verse;

namespace SolarWeb.Stratum.Patches.Transpilers;

[HarmonyPatch]
public static class SectionLayer_IndoorMask_Patch
{
  [HarmonyPatch(typeof(SectionLayer_IndoorMask), "HideCommon")]
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
    return map.roofGrid.Roofed(c);
  }
}
