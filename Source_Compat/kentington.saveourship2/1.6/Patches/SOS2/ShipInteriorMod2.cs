using HarmonyLib;
using SaveOurShip2;
using Verse;
using System;

using SolarWeb.Stratum.Stats;
using SolarWeb.Stratum.SOS2.WorldComponents;

namespace SolarWeb.Stratum.SOS2.Patches;

[HarmonyPatch(typeof(ShipInteriorMod2))]
public static class ShipInteriorMod2_Patch
{
  [HarmonyPatch(nameof(ShipInteriorMod2.IsRoofDefAirtight))]
  [HarmonyPostfix]
  public static void IsRoofDefAirtight_Postfix(RoofDef roof, ref bool __result)
  {
    if (!__result && RoofStatCache.IsCustomRoof(roof))
    {
      // We can only check base airtightness here as we lack cell context.
      // Cell-specific airtightness (stuff-based) is handled in SpaceRoomCheck_Postfix.
      __result = RoofStatCache.GetIsAirtight(roof);
    }
  }

  [HarmonyPatch(nameof(ShipInteriorMod2.MoveShip))]
  [HarmonyPostfix]
  static void MoveShip_Postfix(ShipInteriorMod2 __instance, Building core, Map targetMap, byte rotNum, int __state)
  {
    try
    {
      if (core == null || __state == -1)
        return;

      var actualMap = core.Map;
      if (actualMap == null)
        return;

      var worldComp = ShipInteriorMod2.WorldComp;
      if (worldComp == null)
        return;

      var shipMapComponent = actualMap.GetComponent<ShipMapComp>();
      if (shipMapComponent?.ShipsOnMap == null)
        return;

      if (shipMapComponent.ShipsOnMap.ContainsKey(__state))
      {
        SOS2RoofTracker.RestoreShipRoofs(actualMap, __state, core.Position, rotNum);
      }
    }
    catch (Exception e)
    {
      StratumLog.Warning($"Error restoring SOS2 ship roofs: {e.Message}");
    }
  }

  [HarmonyPatch(nameof(ShipInteriorMod2.MoveShip))]
  [HarmonyPrefix]
  static void MoveShip_Prefix(ShipInteriorMod2 __instance, Building core, Map targetMap, byte rotNum, out int __state)
  {
    __state = -1;
    try
    {
      if (core == null || targetMap == null)
        return;

      var sourceMap = core.Map;
      if (sourceMap == null)
        return;

      var worldComp = ShipInteriorMod2.WorldComp;
      if (worldComp == null)
        return;

      var shipMapComponent = sourceMap.GetComponent<ShipMapComp>();
      if (shipMapComponent?.ShipsOnMap == null)
        return;

      int shipIndex = -1;
      if (core is Building_ShipBridge bridge)
      {
        shipIndex = bridge.ShipIndex;
      }
      if (shipIndex == -1)
      {
        shipIndex = shipMapComponent.ShipIndexOnVec(core.Position);
      }

      if (shipIndex != -1 && shipMapComponent.ShipsOnMap.TryGetValue(shipIndex, out var ship))
      {
        __state = shipIndex;
        SOS2RoofTracker.CaptureShipRoofs(sourceMap, ship.Area, shipIndex, core.Position);
        foreach (var cell in ship.Area)
        {
          var roof = sourceMap.roofGrid.RoofAt(cell);
          if (roof != null && RoofStatCache.IsCustomRoof(roof))
          {
            sourceMap.roofGrid.SetRoof(cell, null);
          }
        }
      }
    }
    catch (Exception e)
    {
      StratumLog.Warning($"Error capturing SOS2 ship roofs: {e.Message}");
    }
  }
}
