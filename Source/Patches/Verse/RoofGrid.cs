using HarmonyLib;
using Verse;

using SolarWeb.Stratum.Stats;
using SolarWeb.Stratum.Hooks;
using SolarWeb.Stratum.MapComponents;

namespace SolarWeb.Stratum.Patches;

[HarmonyPatch]
public static class RoofGrid_Patch
{
  [HarmonyPatch(typeof(RoofGrid), nameof(RoofGrid.GetCellBool))]
  [HarmonyPrefix]
  public static bool GetCellBool_Prefix(RoofGrid __instance, int index, ref bool __result)
  {
    var roof = __instance.RoofAt(index);
    if (RoofStatCache.IsCustomRoof(roof))
    {
      __result = false;
      return false;
    }
    return true;
  }

  [HarmonyPatch(typeof(RoofGrid), nameof(RoofGrid.Roofed), [typeof(IntVec3)])]
  [HarmonyPrefix]
  public static bool Roofed_Prefix(RoofGrid __instance, IntVec3 c, ref bool __result)
  {
    var roof = __instance.RoofAt(c);
    if (roof != null && RoofStatCache.IsCustomRoof(roof))
    {
      __result = true;
      return false;
    }
    return true;
  }

  [HarmonyPatch(typeof(RoofGrid), nameof(RoofGrid.SetRoof))]
  [HarmonyPrefix]
  public static bool SetRoof_Prefix(IntVec3 c, ref RoofDef def, Map ___map, out RoofDef? __state)
  {
    if (___map == null || ___map.roofGrid == null)
    {
      __state = null;
      return true;
    }

    var oldRoof = ___map.roofGrid.RoofAt(c);
    __state = oldRoof;

    var registry = MapHookRegistry.Get(___map);
    if (registry != null)
    {
      bool allow = true;
      RoofDef? newRoof = def;
      var handlers = registry.GetHandlers<MapHookRegistry.BeforeSetRoofHandler>(MapHookRegistry.HookId.BeforeSetRoof);
      if (handlers != null)
      {
        for (int i = 0; i < handlers.Count; i++)
        {
          try
          {
            handlers[i](___map, c, oldRoof, ref newRoof, ref allow);
          }
          catch (System.Exception ex)
          {
            StratumLog.Error($"Error in BeforeSetRoof subscriber: {ex}");
          }
        }
      }
      def = newRoof!;
      if (!allow)
      {
        return false;
      }
    }
    return true;
  }

  [HarmonyPatch(typeof(RoofGrid), nameof(RoofGrid.SetRoof))]
  [HarmonyPostfix]
  public static void SetRoof_Postfix(IntVec3 c, Map ___map, RoofDef? __state)
  {
    if (___map == null || ___map.roofGrid == null) return;
    var currentRoof = ___map.roofGrid.RoofAt(c);
    if (currentRoof == __state) return;

    var globalHandlers = MapHookRegistry.GetGlobalHandlers<MapHookRegistry.RoofChangedHandler>(MapHookRegistry.HookId.RoofChanged);
    if (globalHandlers != null)
    {
      for (int i = 0; i < globalHandlers.Count; i++)
      {
        try
        {
          globalHandlers[i](___map, c, __state, currentRoof);
        }
        catch (System.Exception ex)
        {
          StratumLog.Error($"Error in global RoofChanged subscriber: {ex}");
        }
      }
    }

    var registry = MapHookRegistry.Get(___map);
    if (registry != null)
    {
      var handlers = registry.GetHandlers<MapHookRegistry.RoofChangedHandler>(MapHookRegistry.HookId.RoofChanged);
      if (handlers != null)
      {
        for (int i = 0; i < handlers.Count; i++)
        {
          try
          {
            handlers[i](___map, c, __state, currentRoof);
          }
          catch (System.Exception ex)
          {
            StratumLog.Error($"Error in RoofChanged subscriber: {ex}");
          }
        }
      }
    }

    if (___map.areaManager.NoRoof != null)
    {
      ___map.areaManager.NoRoof[c] = false;
    }
    if (___map.areaManager.BuildRoof != null)
    {
      ___map.areaManager.BuildRoof[c] = false;
    }

    if (___map.regionAndRoomUpdater != null && ___map.regionAndRoomUpdater.Enabled)
    {
      var room = c.GetRoom(___map);
      if (room != null)
      {
        foreach (var district in room.Districts)
        {
          district.Notify_RoofChanged();
        }
      }
    }

    var integrity = ___map.GetComponent<RoofIntegrityGrid>();
    if (currentRoof != null && RoofStatCache.IsCustomRoof(currentRoof))
    {
      ThingDef? stuff = integrity?.GetStuff(c);
      UnityEngine.Color? tint = null;
      if (DebugSettings.godMode)
      {
        var designator = Find.DesignatorManager.SelectedDesignator as AI.Designators.BuildCustomRoof;
        if (designator != null)
        {
          stuff = designator.StuffDef;
          tint = designator.SelectedTint;
        }
      }

      if (stuff == null && currentRoof.isNatural)
      {
        stuff = RoofIntegrityGrid.GetStonyStuffForCell(currentRoof, c, ___map);
      }

      if (stuff == null && GravshipPlacementUtility_SpawnRoofs_Patch.CurrentLandingGravship != null)
      {
        var local = c - GravshipPlacementUtility_SpawnRoofs_Patch.CurrentLandingRoot;
        if (GravshipPlacementUtility_SpawnRoofs_Patch.CurrentRoofData != null &&
            GravshipPlacementUtility_SpawnRoofs_Patch.CurrentRoofData.TryGetValue(local, out var cellData))
        {
          stuff = cellData.stuff;
          integrity?.InitializeRoof(c, currentRoof, stuff, cellData.glassTint, cellData.hitPoints);
        }
        else
        {
          integrity?.InitializeRoof(c, currentRoof, stuff, tint);
        }
      }
      else
      {
        integrity?.InitializeRoof(c, currentRoof, stuff, tint);
      }
    }
    else
    {
      integrity?.RemoveRoof(c);
    }

    if (Find.Selector.SelectedObjects.Count > 0)
    {
      for (int i = Find.Selector.SelectedObjects.Count - 1; i >= 0; i--)
      {
        if (Find.Selector.SelectedObjects[i] is UI.SelectedRoof sr && sr.map == ___map && sr.cell == c)
        {
          if (currentRoof == null || sr.def != currentRoof)
          {
            Find.Selector.Deselect(sr);
          }
        }
      }
    }
  }
}
