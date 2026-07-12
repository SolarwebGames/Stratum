using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using HarmonyLib;
using Verse;

using SolarWeb.Stratum.Stats;

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
    return map.roofGrid.Roofed(c);
  }

  public static bool IsProperRoomForMask(Room room, IntVec3 cell)
  {
    if (room != null)
    {
      var map = room.Map;
      if (map != null)
      {
        var roof = map.roofGrid.RoofAt(cell);
        if (roof != null && RoofStatCache.IsCustomRoof(roof) && RoofStatCache.GetEffectiveTransparency(roof, map, cell) > 0f)
        {
          return false;
        }
      }
      return room.ProperRoom;
    }
    return false;
  }

  [HarmonyPatch("GenerateSectionLayer")]
  [HarmonyTranspiler]
  public static IEnumerable<CodeInstruction> GenerateSectionLayer_Transpiler(IEnumerable<CodeInstruction> instructions)
  {
    var codes = new List<CodeInstruction>(instructions);

    var getRoomMethod = AccessTools.Method(typeof(GridsUtility), nameof(GridsUtility.GetRoom), [typeof(IntVec3), typeof(Map)]);
    var properRoomGetter = AccessTools.PropertyGetter(typeof(Room), nameof(Room.ProperRoom));
    var helperMethod = AccessTools.Method(typeof(SectionLayer_IndoorMask_Patch), nameof(IsProperRoomForMask));

    CodeInstruction? loadCellInstruction = null;

    for (int i = 0; i < codes.Count; i++)
    {
      if (codes[i].Calls(getRoomMethod))
      {
        for (int j = i - 1; j >= 0; j--)
        {
          if (codes[j].opcode == OpCodes.Ldloc || codes[j].opcode == OpCodes.Ldloc_S ||
              codes[j].opcode == OpCodes.Ldloc_0 || codes[j].opcode == OpCodes.Ldloc_1 ||
              codes[j].opcode == OpCodes.Ldloc_2 || codes[j].opcode == OpCodes.Ldloc_3)
          {
            loadCellInstruction = codes[j].Clone();
            break;
          }
        }
      }

      if (codes[i].Calls(properRoomGetter))
      {
        if (loadCellInstruction != null)
        {
          codes.Insert(i, loadCellInstruction);
          i++;

          codes[i].opcode = OpCodes.Call;
          codes[i].operand = helperMethod;
        }
      }
    }

    return codes.AsEnumerable();
  }
}
