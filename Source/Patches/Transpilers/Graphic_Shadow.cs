using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using HarmonyLib;
using Verse;

using SolarWeb.Stratum.Stats;

namespace SolarWeb.Stratum.Patches.Transpilers;

[HarmonyPatch]
public static class Graphic_Shadow_Patch
{
  [HarmonyPatch(typeof(Graphic_Shadow), nameof(Graphic_Shadow.DrawWorker))]
  [HarmonyTranspiler]
  public static IEnumerable<CodeInstruction> DrawWorker_Transpiler(IEnumerable<CodeInstruction> instructions)
  {
    var codes = new List<CodeInstruction>(instructions);
    var roofedMethod = AccessTools.Method(typeof(RoofGrid), nameof(RoofGrid.Roofed), [typeof(IntVec3)]);
    var helperMethod = AccessTools.Method(typeof(Graphic_Shadow_Patch), nameof(ShouldBlockShadow));

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

  public static bool ShouldBlockShadow(RoofGrid grid, IntVec3 c)
  {
    var roof = grid.RoofAt(c);
    if (roof == null) return false;
    return RoofStatCache.GetTransparency(roof) <= 0f;
  }
}
