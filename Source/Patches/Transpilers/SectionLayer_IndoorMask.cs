using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using HarmonyLib;
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

  // Treat transparent roofs as roofed under normal gameplay to prevent weather rendering inside
  public static bool IsRoofedForMask(IntVec3 c, Map map)
  {
    return map.roofGrid.Roofed(c);
  }

  // If the roof overlay is enabled, disable the indoor mask so weather renders over the roofs
  [HarmonyPatch(nameof(SectionLayer_IndoorMask.Visible), MethodType.Getter)]
  [HarmonyPrefix]
  public static bool Visible_Prefix(ref bool __result)
  {
    if (Find.PlaySettings.showRoofOverlay)
    {
      __result = false;
      return false;
    }
    return true;
  }
}
