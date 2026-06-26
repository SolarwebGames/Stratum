using System;
using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace SolarWeb.Stratum.Hooks;

public class MapHookRegistry : MapComponent
{
  private static readonly Dictionary<Map, MapHookRegistry> cache = new();

  public static MapHookRegistry? Get(Map map)
  {
    if (map == null) return null;
    if (cache.TryGetValue(map, out var registry))
    {
      return registry;
    }
    var component = map.GetComponent<MapHookRegistry>();
    if (component != null)
    {
      cache[map] = component;
    }
    return component;
  }

  public MapHookRegistry(Map map) : base(map)
  {
    cache[map] = this;
  }

  public override void MapRemoved()
  {
    base.MapRemoved();
    cache.Remove(map);
  }

  public override void FinalizeInit()
  {
    base.FinalizeInit();
    cache[map] = this;
  }

  public delegate void RoofChangedHandler(Map map, IntVec3 cell, RoofDef? oldRoof, RoofDef? newRoof);
  private readonly List<RoofChangedHandler> onRoofChanged = new();
  public event RoofChangedHandler OnRoofChanged
  {
    add => onRoofChanged.Add(value);
    remove => onRoofChanged.Remove(value);
  }

  public delegate void BeforeSetRoofHandler(Map map, IntVec3 cell, RoofDef? oldRoof, ref RoofDef? newRoof, ref bool allow);
  private readonly List<BeforeSetRoofHandler> onBeforeSetRoof = new();
  public event BeforeSetRoofHandler OnBeforeSetRoof
  {
    add => onBeforeSetRoof.Add(value);
    remove => onBeforeSetRoof.Remove(value);
  }

  public delegate bool AirtightCheckHandler(RoofDef def);
  private static readonly List<AirtightCheckHandler> globalAirtightCheck = new();
  public static event AirtightCheckHandler GlobalAirtightCheck
  {
    add => globalAirtightCheck.Add(value);
    remove => globalAirtightCheck.Remove(value);
  }

  public delegate float TransparencyCheckHandler(RoofDef def);
  private static readonly List<TransparencyCheckHandler> globalTransparencyCheck = new();
  public static event TransparencyCheckHandler GlobalTransparencyCheck
  {
    add => globalTransparencyCheck.Add(value);
    remove => globalTransparencyCheck.Remove(value);
  }

  public delegate void RoofDebrisDroppedHandler(Map map, IntVec3 cell, RoofDef roofDef, ThingDef? stuffDef);
  private readonly List<RoofDebrisDroppedHandler> onRoofDebrisDropped = new();
  public event RoofDebrisDroppedHandler OnRoofDebrisDropped
  {
    add => onRoofDebrisDropped.Add(value);
    remove => onRoofDebrisDropped.Remove(value);
  }

  public delegate void PowerNetEnergyGainHandler(RimWorld.PowerNet net, ref float energyGainRate);
  private readonly List<PowerNetEnergyGainHandler> onCalculateEnergyGainRate = new();
  public event PowerNetEnergyGainHandler OnCalculateEnergyGainRate
  {
    add => onCalculateEnergyGainRate.Add(value);
    remove => onCalculateEnergyGainRate.Remove(value);
  }

  public delegate bool? RoofBuildingRenderCheckHandler(Thing thing);
  private readonly List<RoofBuildingRenderCheckHandler> onRoofBuildingRenderCheck = new();
  public event RoofBuildingRenderCheckHandler OnRoofBuildingRenderCheck
  {
    add => onRoofBuildingRenderCheck.Add(value);
    remove => onRoofBuildingRenderCheck.Remove(value);
  }

  public delegate bool? RoofBuildingSelectableCheckHandler(Thing thing);
  private readonly List<RoofBuildingSelectableCheckHandler> onRoofBuildingSelectableCheck = new();
  public event RoofBuildingSelectableCheckHandler OnRoofBuildingSelectableCheck
  {
    add => onRoofBuildingSelectableCheck.Add(value);
    remove => onRoofBuildingSelectableCheck.Remove(value);
  }

  public delegate bool RoofBuildingDestroyedDebrisHandler(Thing thing, Map map);
  private readonly List<RoofBuildingDestroyedDebrisHandler> onRoofBuildingDestroyedDebris = new();
  public event RoofBuildingDestroyedDebrisHandler OnRoofBuildingDestroyedDebris
  {
    add => onRoofBuildingDestroyedDebris.Add(value);
    remove => onRoofBuildingDestroyedDebris.Remove(value);
  }

  public delegate bool? RoofBuildingPlacementCheckHandler(BuildableDef def, IntVec3 cell, Map map);
  private readonly List<RoofBuildingPlacementCheckHandler> onRoofBuildingPlacementCheck = new();
  public event RoofBuildingPlacementCheckHandler OnRoofBuildingPlacementCheck
  {
    add => onRoofBuildingPlacementCheck.Add(value);
    remove => onRoofBuildingPlacementCheck.Remove(value);
  }

  public delegate Vector3? RoofBuildingDrawPosHandler(Thing thing, Vector3 originalDrawPos);
  private readonly List<RoofBuildingDrawPosHandler> onRoofBuildingDrawPos = new();
  public event RoofBuildingDrawPosHandler OnRoofBuildingDrawPos
  {
    add => onRoofBuildingDrawPos.Add(value);
    remove => onRoofBuildingDrawPos.Remove(value);
  }

  public delegate Vector3? RoofBuildingTrueCenterHandler(Thing thing, Vector3 originalTrueCenter);
  private readonly List<RoofBuildingTrueCenterHandler> onRoofBuildingTrueCenter = new();
  public event RoofBuildingTrueCenterHandler OnRoofBuildingTrueCenter
  {
    add => onRoofBuildingTrueCenter.Add(value);
    remove => onRoofBuildingTrueCenter.Remove(value);
  }

  public delegate bool? BlocksConstructionHandler(Thing constructible, Thing existingThing);
  private readonly List<BlocksConstructionHandler> onBlocksConstruction = new();
  public event BlocksConstructionHandler OnBlocksConstruction
  {
    add => onBlocksConstruction.Add(value);
    remove => onBlocksConstruction.Remove(value);
  }

  public delegate void PlaySettingsDoMapControlsHandler(WidgetRow row);
  private static readonly List<PlaySettingsDoMapControlsHandler> onPlaySettingsDoMapControls = new();
  public static event PlaySettingsDoMapControlsHandler OnPlaySettingsDoMapControls
  {
    add => onPlaySettingsDoMapControls.Add(value);
    remove => onPlaySettingsDoMapControls.Remove(value);
  }

  public void Notify_RoofChanged(IntVec3 cell, RoofDef? oldRoof, RoofDef? newRoof)
  {
    for (int i = 0; i < onRoofChanged.Count; i++)
    {
      try
      {
        onRoofChanged[i](map, cell, oldRoof, newRoof);
      }
      catch (Exception ex)
      {
        StratumLog.Error($"Error in OnRoofChanged subscriber: {ex}");
      }
    }
  }

  public void Notify_BeforeSetRoof(IntVec3 cell, RoofDef? oldRoof, ref RoofDef? newRoof, ref bool allow)
  {
    for (int i = 0; i < onBeforeSetRoof.Count; i++)
    {
      try
      {
        onBeforeSetRoof[i](map, cell, oldRoof, ref newRoof, ref allow);
      }
      catch (Exception ex)
      {
        StratumLog.Error($"Error in OnBeforeSetRoof subscriber: {ex}");
      }
    }
  }

  public void Notify_RoofDebrisDropped(IntVec3 cell, RoofDef roofDef, ThingDef? stuffDef)
  {
    for (int i = 0; i < onRoofDebrisDropped.Count; i++)
    {
      try
      {
        onRoofDebrisDropped[i](map, cell, roofDef, stuffDef);
      }
      catch (Exception ex)
      {
        StratumLog.Error($"Error in OnRoofDebrisDropped subscriber: {ex}");
      }
    }
  }

  public void Notify_CalculateEnergyGainRate(RimWorld.PowerNet net, ref float energyGainRate)
  {
    for (int i = 0; i < onCalculateEnergyGainRate.Count; i++)
    {
      try
      {
        onCalculateEnergyGainRate[i](net, ref energyGainRate);
      }
      catch (Exception ex)
      {
        StratumLog.Error($"Error in OnCalculateEnergyGainRate subscriber: {ex}");
      }
    }
  }

  public static void Notify_PlaySettingsDoMapControls(WidgetRow row)
  {
    for (int i = 0; i < onPlaySettingsDoMapControls.Count; i++)
    {
      try
      {
        onPlaySettingsDoMapControls[i](row);
      }
      catch (Exception ex)
      {
        StratumLog.Error($"Error in OnPlaySettingsDoMapControls subscriber: {ex}");
      }
    }
    try
    {
      Utilities.RoofBuildings.DoMapControls(row);
    }
    catch (Exception ex)
    {
      StratumLog.Error($"Error in built-in DoMapControls: {ex}");
    }
  }

  public bool? CheckRoofBuildingRender(Thing thing)
  {
    for (int i = 0; i < onRoofBuildingRenderCheck.Count; i++)
    {
      try
      {
        var result = onRoofBuildingRenderCheck[i](thing);
        if (result.HasValue) return result;
      }
      catch (Exception ex)
      {
        StratumLog.Error($"Error in OnRoofBuildingRenderCheck subscriber: {ex}");
      }
    }
    try
    {
      return Utilities.RoofBuildings.CheckRoofBuildingRender(thing, map);
    }
    catch (Exception ex)
    {
      StratumLog.Error($"Error in built-in CheckRoofBuildingRender: {ex}");
      return null;
    }
  }

  public bool? CheckRoofBuildingSelectable(Thing thing)
  {
    for (int i = 0; i < onRoofBuildingSelectableCheck.Count; i++)
    {
      try
      {
        var result = onRoofBuildingSelectableCheck[i](thing);
        if (result.HasValue) return result;
      }
      catch (Exception ex)
      {
        StratumLog.Error($"Error in OnRoofBuildingSelectableCheck subscriber: {ex}");
      }
    }
    try
    {
      return Utilities.RoofBuildings.CheckRoofBuildingSelectable(thing);
    }
    catch (Exception ex)
    {
      StratumLog.Error($"Error in built-in CheckRoofBuildingSelectable: {ex}");
      return null;
    }
  }

  public bool CheckRoofBuildingDestroyedDebris(Thing thing)
  {
    bool handled = false;
    for (int i = 0; i < onRoofBuildingDestroyedDebris.Count; i++)
    {
      try
      {
        if (onRoofBuildingDestroyedDebris[i](thing, map))
        {
          handled = true;
        }
      }
      catch (Exception ex)
      {
        StratumLog.Error($"Error in OnRoofBuildingDestroyedDebris subscriber: {ex}");
      }
    }
    if (handled) return true;
    try
    {
      return Utilities.RoofBuildings.DropDebris(thing, map);
    }
    catch (Exception ex)
    {
      StratumLog.Error($"Error in built-in DropDebris: {ex}");
      return false;
    }
  }

  public bool? CheckRoofBuildingPlacement(BuildableDef def, IntVec3 cell)
  {
    for (int i = 0; i < onRoofBuildingPlacementCheck.Count; i++)
    {
      try
      {
        var result = onRoofBuildingPlacementCheck[i](def, cell, map);
        if (result.HasValue) return result;
      }
      catch (Exception ex)
      {
        StratumLog.Error($"Error in OnRoofBuildingPlacementCheck subscriber: {ex}");
      }
    }
    return null;
  }

  public Vector3? GetRoofBuildingDrawPos(Thing thing, Vector3 originalDrawPos)
  {
    for (int i = 0; i < onRoofBuildingDrawPos.Count; i++)
    {
      try
      {
        var result = onRoofBuildingDrawPos[i](thing, originalDrawPos);
        if (result.HasValue) return result;
      }
      catch (Exception ex)
      {
        StratumLog.Error($"Error in OnRoofBuildingDrawPos subscriber: {ex}");
      }
    }
    try
    {
      return Utilities.RoofBuildings.GetRoofBuildingDrawPos(thing, originalDrawPos);
    }
    catch (Exception ex)
    {
      StratumLog.Error($"Error in built-in GetRoofBuildingDrawPos: {ex}");
      return null;
    }
  }

  public Vector3? GetRoofBuildingTrueCenter(Thing thing, Vector3 originalTrueCenter)
  {
    for (int i = 0; i < onRoofBuildingTrueCenter.Count; i++)
    {
      try
      {
        var result = onRoofBuildingTrueCenter[i](thing, originalTrueCenter);
        if (result.HasValue) return result;
      }
      catch (Exception ex)
      {
        StratumLog.Error($"Error in OnRoofBuildingTrueCenter subscriber: {ex}");
      }
    }
    try
    {
      return Utilities.RoofBuildings.GetRoofBuildingTrueCenter(thing, originalTrueCenter);
    }
    catch (Exception ex)
    {
      StratumLog.Error($"Error in built-in GetRoofBuildingTrueCenter: {ex}");
      return null;
    }
  }

  public bool? CheckBlocksConstruction(Thing constructible, Thing existingThing)
  {
    for (int i = 0; i < onBlocksConstruction.Count; i++)
    {
      try
      {
        var result = onBlocksConstruction[i](constructible, existingThing);
        if (result.HasValue) return result;
      }
      catch (Exception ex)
      {
        StratumLog.Error($"Error in OnBlocksConstruction subscriber: {ex}");
      }
    }
    try
    {
      return Utilities.RoofBuildings.CheckBlocksConstruction(constructible, existingThing);
    }
    catch (Exception ex)
    {
      StratumLog.Error($"Error in built-in CheckBlocksConstruction: {ex}");
      return null;
    }
  }

  public static bool IsAirtightOverride(RoofDef def)
  {
    if (def == null) return false;
    for (int i = 0; i < globalAirtightCheck.Count; i++)
    {
      try
      {
        if (globalAirtightCheck[i](def)) return true;
      }
      catch (Exception ex)
      {
        StratumLog.Error($"Error in GlobalAirtightCheck subscriber: {ex}");
      }
    }
    return false;
  }

  public static float GetTransparencyOverride(RoofDef def)
  {
    if (def == null) return 0f;
    float max = 0f;
    for (int i = 0; i < globalTransparencyCheck.Count; i++)
    {
      try
      {
        max = Math.Max(max, globalTransparencyCheck[i](def));
      }
      catch (Exception ex)
      {
        StratumLog.Error($"Error in GlobalTransparencyCheck subscriber: {ex}");
      }
    }
    return max;
  }
}
