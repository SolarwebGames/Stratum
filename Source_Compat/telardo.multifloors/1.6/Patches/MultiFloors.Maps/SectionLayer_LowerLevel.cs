using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using RimWorld;
using Verse;
using global::MultiFloors;
using global::MultiFloors.Maps;

using SolarWeb.Stratum.MultiFloors.Rendering;
using SolarWeb.Stratum.Stats;

namespace SolarWeb.Stratum.MultiFloors.Patches.MultiFloors.Maps;

[HarmonyPatch(typeof(SectionLayer_LowerLevel))]
public static class SectionLayer_LowerLevel_Patch
{
  /// <summary>
  /// Does this roof occlude the level below it, as far as MultiFloors' lower-level renderer is
  /// concerned?
  /// </summary>
  /// <remarks>
  /// Skylights deliberately answer <c>false</c>. MultiFloors stops descending at the first
  /// occluding roof and then suppresses the lower level's terrain and things entirely, so treating
  /// glass as occluding would mean looking "through" it at nothing. Returning false keeps
  /// MultiFloors descending so the room below renders, and
  /// <see cref="LowerLevelRoofPainter.PrintSkylightGlass"/> lays the glass over the top afterwards.
  /// </remarks>
  public static bool IsConstructedOrCustomRoof(RoofDef roof)
  {
    if (roof == null) return false;
    if (RoofStatCache.IsSkylight(roof)) return false;
    if (roof == RoofDefOf.RoofConstructed) return true;
    return RoofStatCache.IsCustomRoof(roof);
  }

  /// <summary>
  /// Paints skylight glass over a lower level that MultiFloors has just finished drawing.
  /// </summary>
  /// <remarks>
  /// <c>PrintThings</c> is the natural hook: it already receives the resolved lower map and the
  /// cell, it runs only on the non-occluded path, and it fires right after the level below has been
  /// printed. It is also large enough (and has a try/catch) that it will not be inlined, unlike the
  /// one-line <c>GenerateRoofSubMeshes</c>.
  /// </remarks>
  [HarmonyPatch("PrintThings")]
  [HarmonyPostfix]
  public static void PrintThings_Postfix(SectionLayer_LowerLevel __instance, Map map, IntVec3 cell)
  {
    LowerLevelRoofPainter.PrintSkylightGlass(__instance, cell, map);
  }

  [HarmonyPatch(nameof(SectionLayer_LowerLevel.Regenerate))]
  [HarmonyTranspiler]
  public static IEnumerable<CodeInstruction> Regenerate_Transpiler(IEnumerable<CodeInstruction> instructions)
  {
    var list = new List<CodeInstruction>(instructions);

    // The two rewrites are independent and independently reported, so a future MultiFloors change
    // that breaks one does not silently take the other with it. Each is isolated: an exception
    // escaping a transpiler fails the whole patch, which would cost us the other rewrite and leave
    // MultiFloors' Regenerate unpatched entirely.
    try
    {
      RewriteRoofDefTest(list);
    }
    catch (System.Exception ex)
    {
      StratumLog.Error($"MultiFloors compat: roof-def rewrite failed, lower-level roofs will not recognise Stratum roofs: {ex}");
    }

    try
    {
      RedirectRoofPainting(list);
    }
    catch (System.Exception ex)
    {
      StratumLog.Error($"MultiFloors compat: roof-painting redirect failed, lower-level roofs keep MultiFloors' generic texture: {ex}");
    }

    return list;
  }

  /// <summary>
  /// Rewrites <c>RoofAt(item) == RoofDefOf.RoofConstructed</c> into a call to
  /// <see cref="IsConstructedOrCustomRoof"/> so Stratum roofs are recognised as occluding.
  /// </summary>
  private static void RewriteRoofDefTest(List<CodeInstruction> list)
  {
    var targetField = AccessTools.Field(typeof(RoofDefOf), nameof(RoofDefOf.RoofConstructed));
    var helperMethod = AccessTools.Method(typeof(SectionLayer_LowerLevel_Patch), nameof(IsConstructedOrCustomRoof));

    int replacements = 0;

    for (int i = 0; i < list.Count; i++)
    {
      if (!list[i].LoadsField(targetField)) continue;
      if (i + 1 >= list.Count) continue;

      var next = list[i + 1];

      // The source reads `map.roofGrid.RoofAt(item) == RoofDefOf.RoofConstructed`, but Roslyn does
      // not always emit that as an explicit comparison. Inside the surrounding && chain it compiles
      // to a branch (bne.un.s), so matching only ceq/op_Equality patches nothing at all.
      // In every form the roof reference is already on the stack ahead of the ldsfld, so replacing
      // the ldsfld with a call to the helper consumes it and leaves a bool behind. The comparison
      // then collapses into a plain boolean test, which keeps the stack balanced.
      OpCode? branchOpcode;

      if (next.opcode == OpCodes.Ceq ||
          (next.opcode == OpCodes.Call && next.operand is MethodInfo mi && mi.Name == "op_Equality"))
      {
        branchOpcode = null;
      }
      else if (next.opcode == OpCodes.Beq || next.opcode == OpCodes.Beq_S)
      {
        branchOpcode = OpCodes.Brtrue;
      }
      else if (next.opcode == OpCodes.Bne_Un || next.opcode == OpCodes.Bne_Un_S)
      {
        branchOpcode = OpCodes.Brfalse;
      }
      else
      {
        continue;
      }

      // Carry over any labels and exception blocks anchored to the rewritten instructions,
      // otherwise branches targeting them are left dangling.
      var call = new CodeInstruction(OpCodes.Call, helperMethod);
      call.labels.AddRange(list[i].labels);
      call.blocks.AddRange(list[i].blocks);
      list[i] = call;

      if (branchOpcode.HasValue)
      {
        var branch = new CodeInstruction(branchOpcode.Value, next.operand);
        branch.labels.AddRange(next.labels);
        branch.blocks.AddRange(next.blocks);
        list[i + 1] = branch;
      }
      else
      {
        if (next.labels.Count > 0 && i + 2 < list.Count)
        {
          list[i + 2].labels.AddRange(next.labels);
        }
        list.RemoveAt(i + 1);
      }

      replacements++;
    }

    if (replacements == 0)
    {
      StratumLog.Error(
        "MultiFloors compat: the SectionLayer_LowerLevel.Regenerate transpiler matched no " +
        "RoofDefOf.RoofConstructed comparison. Stratum custom roofs will not be recognised by the " +
        "lower-level renderer -- MultiFloors has likely changed this method.");
    }
  }

  /// <summary>
  /// Redirects the <c>GenerateRoofSubMeshes(cell)</c> call to Stratum's painter, pushing the
  /// resolved lower map as an extra argument.
  /// </summary>
  /// <remarks>
  /// <c>GenerateRoofSubMeshes</c> only receives the cell; the map the descend loop settled on lives
  /// in a local. Patching the method directly would not help (and it is a one-line method, so a
  /// prefix risks being inlined away), hence rewriting the call site instead.
  /// </remarks>
  private static void RedirectRoofPainting(List<CodeInstruction> list)
  {
    var lowerMapMethod = AccessTools.Method(typeof(PrepatcherFields), nameof(PrepatcherFields.LowerMap));
    var generateRoof = AccessTools.Method(typeof(SectionLayer_LowerLevel), "GenerateRoofSubMeshes");
    var painter = AccessTools.Method(typeof(LowerLevelRoofPainter), nameof(LowerLevelRoofPainter.PrintOpaqueRoof));

    if (lowerMapMethod == null || generateRoof == null || painter == null)
    {
      StratumLog.Error(
        "MultiFloors compat: could not resolve the methods needed to redirect lower-level roof " +
        "painting. Stratum roofs one level down will keep MultiFloors' generic texture.");
      return;
    }

    // Find the local the descend loop stores map.LowerMap() into. There are two LowerMap() call
    // sites in this method; only the loop's is followed by a store, so the anchor is unambiguous.
    // LowerMap is a Prepatcher `extern ref Map`, so a ldind.ref sits between the call and the store.
    CodeInstruction? mapStore = null;
    for (int i = 0; i < list.Count && mapStore == null; i++)
    {
      // Calls() compares with Equals rather than reference identity: reflection can hand back
      // distinct MethodInfo instances for the same method.
      if (!list[i].Calls(lowerMapMethod)) continue;

      for (int j = i + 1; j < list.Count && j <= i + 3; j++)
      {
        if (list[j].IsStloc())
        {
          mapStore = list[j];
          break;
        }
        if (list[j].opcode != OpCodes.Ldind_Ref) break;
      }
    }

    if (mapStore == null)
    {
      StratumLog.Error(
        "MultiFloors compat: could not locate the lower-map local in " +
        "SectionLayer_LowerLevel.Regenerate. Stratum roofs one level down will keep MultiFloors' " +
        "generic texture.");
      return;
    }

    var loadMap = LoadCounterpart(mapStore);
    if (loadMap == null)
    {
      StratumLog.Error($"MultiFloors compat: unsupported local store opcode '{mapStore.opcode}' for the lower-map local.");
      return;
    }

    for (int i = 0; i < list.Count; i++)
    {
      if (!list[i].Calls(generateRoof)) continue;

      // Stack already holds (this, cell); appending the map gives PrintOpaqueRoof's signature.
      var call = new CodeInstruction(OpCodes.Call, painter);
      call.labels.AddRange(list[i].labels);
      call.blocks.AddRange(list[i].blocks);
      list[i] = call;
      list.Insert(i, loadMap);
      return;
    }

    StratumLog.Error(
      "MultiFloors compat: could not locate the GenerateRoofSubMeshes call site in " +
      "SectionLayer_LowerLevel.Regenerate. Stratum roofs one level down will keep MultiFloors' " +
      "generic texture.");
  }

  private static CodeInstruction? LoadCounterpart(CodeInstruction store)
  {
    if (store.opcode == OpCodes.Stloc_0) return new CodeInstruction(OpCodes.Ldloc_0);
    if (store.opcode == OpCodes.Stloc_1) return new CodeInstruction(OpCodes.Ldloc_1);
    if (store.opcode == OpCodes.Stloc_2) return new CodeInstruction(OpCodes.Ldloc_2);
    if (store.opcode == OpCodes.Stloc_3) return new CodeInstruction(OpCodes.Ldloc_3);
    if (store.opcode == OpCodes.Stloc_S) return new CodeInstruction(OpCodes.Ldloc_S, store.operand);
    if (store.opcode == OpCodes.Stloc) return new CodeInstruction(OpCodes.Ldloc, store.operand);
    return null;
  }
}
