using System;
using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace SolarWeb.Stratum.Hooks;

public class MapHookRegistry : MapComponent
{
  public enum HookId
  {
    RoofChanged,
    BeforeSetRoof,
    AirtightCheck,
    TransparencyCheck,
    RoofDebrisDropped,
    PowerNetEnergyGain,
    RoofBuildingRenderCheck,
    RoofBuildingSelectableCheck,
    RoofBuildingPlacementCheck,
    RoofBuildingDrawPos,
    RoofBuildingTrueCenter,
    BlocksConstruction,
    PlaySettingsDoMapControls,
    CanPlaceBlueprintOver,
    CalculateCEDamage,
    RoofDamageCalculation
  }

  private static readonly Dictionary<Map, MapHookRegistry> cache = [];

  private readonly Dictionary<HookId, object> instanceHooks = [];

  private static readonly Dictionary<HookId, object> globalHooks = [];

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

  public void Register<T>(HookId hookId, T handler) where T : Delegate
  {
    if (!instanceHooks.TryGetValue(hookId, out var listObj))
    {
      listObj = new List<T>();
      instanceHooks[hookId] = listObj;
    }
    var list = (List<T>)listObj;
    list.Add(handler);
  }

  public void Unregister<T>(HookId hookId, T handler) where T : Delegate
  {
    if (instanceHooks.TryGetValue(hookId, out var listObj))
    {
      var list = (List<T>)listObj;
      list.Remove(handler);
    }
  }

  public List<T>? GetHandlers<T>(HookId hookId) where T : Delegate
  {
    if (instanceHooks.TryGetValue(hookId, out var listObj))
    {
      return (List<T>)listObj;
    }
    return null;
  }

  public static void RegisterGlobal<T>(HookId hookId, T handler) where T : Delegate
  {
    if (!globalHooks.TryGetValue(hookId, out var listObj))
    {
      listObj = new List<T>();
      globalHooks[hookId] = listObj;
    }
    var list = (List<T>)listObj;
    list.Add(handler);
  }

  public static void UnregisterGlobal<T>(HookId hookId, T handler) where T : Delegate
  {
    if (globalHooks.TryGetValue(hookId, out var listObj))
    {
      var list = (List<T>)listObj;
      list.Remove(handler);
    }
  }

  public static List<T>? GetGlobalHandlers<T>(HookId hookId) where T : Delegate
  {
    if (globalHooks.TryGetValue(hookId, out var listObj))
    {
      return (List<T>)listObj;
    }
    return null;
  }

  public delegate void RoofChangedHandler(Map map, IntVec3 cell, RoofDef? oldRoof, RoofDef? newRoof);
  public delegate void BeforeSetRoofHandler(Map map, IntVec3 cell, RoofDef? oldRoof, ref RoofDef? newRoof, ref bool allow);
  public delegate bool AirtightCheckHandler(RoofDef def);
  public delegate float TransparencyCheckHandler(RoofDef def);
  public delegate void RoofDebrisDroppedHandler(Map map, IntVec3 cell, RoofDef roofDef, ThingDef? stuffDef);
  public delegate void PowerNetEnergyGainHandler(RimWorld.PowerNet net, ref float energyGainRate);
  public delegate bool? RoofBuildingRenderCheckHandler(Thing thing);
  public delegate bool? RoofBuildingSelectableCheckHandler(Thing thing);
  public delegate bool? RoofBuildingPlacementCheckHandler(BuildableDef def, IntVec3 cell, Map map);
  public delegate Vector3? RoofBuildingDrawPosHandler(Thing thing, Vector3 originalDrawPos);
  public delegate Vector3? RoofBuildingTrueCenterHandler(Thing thing, Vector3 originalTrueCenter);
  public delegate bool? BlocksConstructionHandler(Thing constructible, Thing existingThing);
  public delegate void PlaySettingsDoMapControlsHandler(WidgetRow row);
  public delegate bool? CanPlaceBlueprintOverHandler(BuildableDef newDef, ThingDef oldDef);
  public delegate bool RoofDamageCalculationHandler(RoofDef roofDef, ThingDef? stuffDef, float baseDamage, float penetration, DamageInfo? damageDef, ref float effectiveDamage);
}
