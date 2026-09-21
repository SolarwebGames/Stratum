using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using SaveOurShip2;
using Verse;

namespace SolarWeb.Stratum.SOS2.Patches;

// Stratum owns roof construction. Suppress only the incidental writes in SOS2
// hull lifecycle methods, not RoofGrid globally or the ship-transfer path.
[HarmonyPatch]
public static class AutomaticShipRoofs
{
  public static IEnumerable<MethodBase> TargetMethods()
  {
    yield return AccessTools.Method(typeof(CompShipCachePart), nameof(CompShipCachePart.PostSpawnSetup));
    yield return AccessTools.Method(typeof(CompShipCachePart), nameof(CompShipCachePart.PostDeSpawn));
    yield return AccessTools.Method(typeof(CompDockExtender), nameof(CompDockExtender.PostSpawnSetup));
    yield return AccessTools.Method(typeof(CompDockExtender), nameof(CompDockExtender.PostDeSpawn));
    yield return AccessTools.Method(typeof(ShipInteriorMod2).Assembly.GetType("SaveOurShip2.CompArchoHullConversion"), "ConvertHullTile");
    // This converter is absent from some SOS2 1.6 builds.
    var gravship = typeof(ShipInteriorMod2).Assembly.GetType("SaveOurShip2.CompGravshipConversion");
    if (gravship != null)
      yield return AccessTools.Method(gravship, "ConvertHullTile");
  }

  public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, MethodBase __originalMethod)
  {
    var setRoof = AccessTools.Method(typeof(RoofGrid), nameof(RoofGrid.SetRoof));
    bool despawning = __originalMethod.Name == nameof(CompShipCachePart.PostDeSpawn);
    var replacement = AccessTools.Method(typeof(AutomaticShipRoofs), despawning ? nameof(RemoveDestroyedRoof) : nameof(KeepExistingRoof));
    int count = 0;
    foreach (var instruction in instructions)
    {
      if (instruction.Calls(setRoof))
      {
        if (despawning)
        {
          // PostDeSpawn(Map, DestroyMode): add the original destruction mode.
          var mode = new CodeInstruction(OpCodes.Ldarg_2);
          mode.labels.AddRange(instruction.labels);
          mode.blocks.AddRange(instruction.blocks);
          instruction.labels.Clear();
          instruction.blocks.Clear();
          yield return mode;
        }
        instruction.opcode = OpCodes.Call;
        instruction.operand = replacement;
        count++;
      }
      yield return instruction;
    }
    int expected = __originalMethod.DeclaringType == typeof(CompShipCachePart)
      && __originalMethod.Name == nameof(CompShipCachePart.PostSpawnSetup) ? 2 : 1;
    if (count != expected)
      throw new InvalidOperationException($"Stratum SOS2 roof patch expected {expected} SetRoof calls in {__originalMethod}, found {count}.");
  }

  public static void KeepExistingRoof(RoofGrid grid, IntVec3 cell, RoofDef roof)
  {
    // No clear-and-restore: even a temporary SetRoof(null) loses Stratum cell data.
  }

  public static void RemoveDestroyedRoof(RoofGrid grid, IntVec3 cell, RoofDef roof, DestroyMode mode)
  {
    // Dismantling, upgrading and despawning hull must not remove a player roof.
    // Actual destruction still breaches it; damage is not a construction operation.
    if (mode == DestroyMode.KillFinalize)
      grid.SetRoof(cell, roof);
  }
}

// Stop SOS2's SetRoof prefix from replacing a player's non-airtight roof choice.
// Returning true as its result allows the real RoofGrid.SetRoof and other mods to run.
[HarmonyPatch(typeof(RebuildShipRoof), nameof(RebuildShipRoof.Prefix))]
public static class KeepPlayerRoofChoice
{
  public static bool Prefix(ref bool __result)
  {
    __result = true;
    return false;
  }
}
