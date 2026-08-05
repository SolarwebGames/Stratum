using System;
using System.Collections.Generic;
using RimWorld;
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
    RoofDamageCalculation,
    ThermalConductivity,
    RoofMaxHitPoints,
    RoofDamageThreshold,
    RoofArmorRating,
    GroundGlow,
    ScanSpeed,
    SkylightTransmission,
    SkylightTint,
    SkyGlowMultiplier,
    SolarPowerOutputFactor,
    DropPodRoofInterception
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

  public static float GetCellThermalConductivity(Map map, IntVec3 cell, float baseConductivity)
  {
    float current = baseConductivity;
    var globalHandlers = GetGlobalHandlers<ThermalConductivityHandler>(HookId.ThermalConductivity);
    if (globalHandlers != null)
    {
      for (int i = 0; i < globalHandlers.Count; i++)
      {
        try
        {
          current = globalHandlers[i](map, cell, current);
        }
        catch (System.Exception ex)
        {
          StratumLog.Error($"Error in global ThermalConductivity handler: {ex}");
        }
      }
    }
    var registry = Get(map);
    if (registry != null)
    {
      var handlers = registry.GetHandlers<ThermalConductivityHandler>(HookId.ThermalConductivity);
      if (handlers != null)
      {
        for (int i = 0; i < handlers.Count; i++)
        {
          try
          {
            current = handlers[i](map, cell, current);
          }
          catch (System.Exception ex)
          {
            StratumLog.Error($"Error in ThermalConductivity handler: {ex}");
          }
        }
      }
    }
    return current;
  }

  public static int GetCellRoofMaxHitPoints(Map map, IntVec3 cell, int baseMaxHp)
  {
    int current = baseMaxHp;
    var globalHandlers = GetGlobalHandlers<RoofMaxHitPointsHandler>(HookId.RoofMaxHitPoints);
    if (globalHandlers != null)
    {
      for (int i = 0; i < globalHandlers.Count; i++)
      {
        try
        {
          current = globalHandlers[i](map, cell, current);
        }
        catch (System.Exception ex)
        {
          StratumLog.Error($"Error in global RoofMaxHitPoints handler: {ex}");
        }
      }
    }
    var registry = Get(map);
    if (registry != null)
    {
      var handlers = registry.GetHandlers<RoofMaxHitPointsHandler>(HookId.RoofMaxHitPoints);
      if (handlers != null)
      {
        for (int i = 0; i < handlers.Count; i++)
        {
          try
          {
            current = handlers[i](map, cell, current);
          }
          catch (System.Exception ex)
          {
            StratumLog.Error($"Error in RoofMaxHitPoints handler: {ex}");
          }
        }
      }
    }
    return current;
  }

  public static float GetCellRoofDamageThreshold(Map map, IntVec3 cell, float baseDt)
  {
    float current = baseDt;
    var globalHandlers = GetGlobalHandlers<RoofDamageThresholdHandler>(HookId.RoofDamageThreshold);
    if (globalHandlers != null)
    {
      for (int i = 0; i < globalHandlers.Count; i++)
      {
        try
        {
          current = globalHandlers[i](map, cell, current);
        }
        catch (System.Exception ex)
        {
          StratumLog.Error($"Error in global RoofDamageThreshold handler: {ex}");
        }
      }
    }
    var registry = Get(map);
    if (registry != null)
    {
      var handlers = registry.GetHandlers<RoofDamageThresholdHandler>(HookId.RoofDamageThreshold);
      if (handlers != null)
      {
        for (int i = 0; i < handlers.Count; i++)
        {
          try
          {
            current = handlers[i](map, cell, current);
          }
          catch (System.Exception ex)
          {
            StratumLog.Error($"Error in RoofDamageThreshold handler: {ex}");
          }
        }
      }
    }
    return current;
  }

  public static float GetCellRoofArmorRating(Map map, IntVec3 cell, float baseArmor)
  {
    float current = baseArmor;
    var globalHandlers = GetGlobalHandlers<RoofArmorRatingHandler>(HookId.RoofArmorRating);
    if (globalHandlers != null)
    {
      for (int i = 0; i < globalHandlers.Count; i++)
      {
        try
        {
          current = globalHandlers[i](map, cell, current);
        }
        catch (System.Exception ex)
        {
          StratumLog.Error($"Error in global RoofArmorRating handler: {ex}");
        }
      }
    }
    var registry = Get(map);
    if (registry != null)
    {
      var handlers = registry.GetHandlers<RoofArmorRatingHandler>(HookId.RoofArmorRating);
      if (handlers != null)
      {
        for (int i = 0; i < handlers.Count; i++)
        {
          try
          {
            current = handlers[i](map, cell, current);
          }
          catch (System.Exception ex)
          {
            StratumLog.Error($"Error in RoofArmorRating handler: {ex}");
          }
        }
      }
    }
    return current;
  }

  public static bool HasSkylightTransmissionHandlers(Map map)
  {
    var globalHandlers = GetGlobalHandlers<SkylightTransmissionHandler>(HookId.SkylightTransmission);
    if (globalHandlers != null && globalHandlers.Count > 0) return true;
    var handlers = Get(map)?.GetHandlers<SkylightTransmissionHandler>(HookId.SkylightTransmission);
    return handlers != null && handlers.Count > 0;
  }

  public static float GetCellSkylightTransmission(Map map, IntVec3 cell, float baseTransmission)
  {
    float current = baseTransmission;
    var globalHandlers = GetGlobalHandlers<SkylightTransmissionHandler>(HookId.SkylightTransmission);
    if (globalHandlers != null)
    {
      for (int i = 0; i < globalHandlers.Count; i++)
      {
        try
        {
          current = globalHandlers[i](map, cell, current);
        }
        catch (System.Exception ex)
        {
          StratumLog.Error($"Error in global SkylightTransmission handler: {ex}");
        }
      }
    }
    var registry = Get(map);
    if (registry != null)
    {
      var handlers = registry.GetHandlers<SkylightTransmissionHandler>(HookId.SkylightTransmission);
      if (handlers != null)
      {
        for (int i = 0; i < handlers.Count; i++)
        {
          try
          {
            current = handlers[i](map, cell, current);
          }
          catch (System.Exception ex)
          {
            StratumLog.Error($"Error in SkylightTransmission handler: {ex}");
          }
        }
      }
    }
    return Mathf.Clamp01(current);
  }

  public static bool HasSkylightTintHandlers(Map map)
  {
    var globalHandlers = GetGlobalHandlers<SkylightTintHandler>(HookId.SkylightTint);
    if (globalHandlers != null && globalHandlers.Count > 0) return true;
    var handlers = Get(map)?.GetHandlers<SkylightTintHandler>(HookId.SkylightTint);
    return handlers != null && handlers.Count > 0;
  }

  public static Color GetCellSkylightTint(Map map, IntVec3 cell, Color baseTint)
  {
    Color current = baseTint;
    var globalHandlers = GetGlobalHandlers<SkylightTintHandler>(HookId.SkylightTint);
    if (globalHandlers != null)
    {
      for (int i = 0; i < globalHandlers.Count; i++)
      {
        try
        {
          current = globalHandlers[i](map, cell, current);
        }
        catch (System.Exception ex)
        {
          StratumLog.Error($"Error in global SkylightTint handler: {ex}");
        }
      }
    }
    var registry = Get(map);
    if (registry != null)
    {
      var handlers = registry.GetHandlers<SkylightTintHandler>(HookId.SkylightTint);
      if (handlers != null)
      {
        for (int i = 0; i < handlers.Count; i++)
        {
          try
          {
            current = handlers[i](map, cell, current);
          }
          catch (System.Exception ex)
          {
            StratumLog.Error($"Error in SkylightTint handler: {ex}");
          }
        }
      }
    }
    // Alpha is meaningless here -- every consumer treats this as a multiplicative RGB filter.
    return new Color(Mathf.Clamp01(current.r), Mathf.Clamp01(current.g), Mathf.Clamp01(current.b), 1f);
  }

  public static bool HasSkyGlowMultiplierHandlers(Map map)
  {
    var globalHandlers = GetGlobalHandlers<SkyGlowMultiplierHandler>(HookId.SkyGlowMultiplier);
    if (globalHandlers != null && globalHandlers.Count > 0) return true;
    var handlers = Get(map)?.GetHandlers<SkyGlowMultiplierHandler>(HookId.SkyGlowMultiplier);
    return handlers != null && handlers.Count > 0;
  }

  public static float GetScanSpeed(Thing scanner, float baseSpeed = 1f)
  {
    if (scanner == null) return baseSpeed;
    float current = baseSpeed;
    if (DefOf.StatDefOf.ScanSpeed != null)
    {
      float statValue = scanner.GetStatValue(DefOf.StatDefOf.ScanSpeed, true);
      current += (statValue - 1f);
    }
    if (DefOf.StatDefOf.ScanSpeedOffset != null)
    {
      float offset = scanner.GetStatValue(DefOf.StatDefOf.ScanSpeedOffset, true);
      current += offset;
    }
    var globalHandlers = GetGlobalHandlers<ScanSpeedHandler>(HookId.ScanSpeed);
    if (globalHandlers != null)
    {
      for (int i = 0; i < globalHandlers.Count; i++)
      {
        try
        {
          current = globalHandlers[i](scanner, current);
        }
        catch (Exception ex)
        {
          StratumLog.Error($"Error in global ScanSpeed handler: {ex}");
        }
      }
    }
    var map = scanner.MapHeld ?? scanner.Map;
    if (map != null)
    {
      var registry = Get(map);
      if (registry != null)
      {
        var handlers = registry.GetHandlers<ScanSpeedHandler>(HookId.ScanSpeed);
        if (handlers != null)
        {
          for (int i = 0; i < handlers.Count; i++)
          {
            try
            {
              current = handlers[i](scanner, current);
            }
            catch (Exception ex)
            {
              StratumLog.Error($"Error in ScanSpeed handler: {ex}");
            }
          }
        }
      }
    }
    return Mathf.Max(0f, current);
  }

  public delegate void RoofChangedHandler(Map map, IntVec3 cell, RoofDef? oldRoof, RoofDef? newRoof);
  public delegate void BeforeSetRoofHandler(Map map, IntVec3 cell, RoofDef? oldRoof, ref RoofDef? newRoof, ref bool allow);
  public delegate bool AirtightCheckHandler(RoofDef def);
  public delegate float TransparencyCheckHandler(RoofDef def);
  public delegate void RoofDebrisDroppedHandler(Map map, IntVec3 cell, RoofDef roofDef, ThingDef? stuffDef);
  public delegate void PowerNetEnergyGainHandler(RimWorld.PowerNet net, ref float energyGainRate);
  public delegate bool? RoofBuildingRenderCheckHandler(Thing thing);
  public delegate bool? RoofBuildingSelectableCheckHandler(Thing thing);
  // Returns null to defer to the next handler / the default checks, or a report to decide the
  // placement. AcceptanceReport rather than bool? so a subscriber can explain the refusal --
  // AcceptanceReport implicitly converts to bool, so a bool? return type silently discarded the
  // reason text and left the player with an unexplained rejection.
  public delegate AcceptanceReport? RoofBuildingPlacementCheckHandler(BuildableDef def, IntVec3 cell, Rot4 rot, Map map);
  public delegate Vector3? RoofBuildingDrawPosHandler(Thing thing, Vector3 originalDrawPos);
  public delegate Vector3? RoofBuildingTrueCenterHandler(Thing thing, Vector3 originalTrueCenter);
  public delegate bool? BlocksConstructionHandler(Thing constructible, Thing existingThing);
  public delegate void PlaySettingsDoMapControlsHandler(WidgetRow row);
  public delegate bool? CanPlaceBlueprintOverHandler(BuildableDef newDef, ThingDef oldDef);
  public delegate bool RoofDamageCalculationHandler(RoofDef roofDef, ThingDef? stuffDef, float baseDamage, float penetration, DamageInfo? damageDef, ref float effectiveDamage);
  public delegate float ThermalConductivityHandler(Map map, IntVec3 cell, float baseConductivity);
  public delegate int RoofMaxHitPointsHandler(Map map, IntVec3 cell, int baseMaxHp);
  public delegate float RoofDamageThresholdHandler(Map map, IntVec3 cell, float baseDt);
  public delegate float RoofArmorRatingHandler(Map map, IntVec3 cell, float baseArmor);
  public delegate bool GroundGlowHandler(GlowGrid instance, Map map, IntVec3 cell, bool ignoreCavePlants, bool ignoreSky, ref float result);
  public delegate float ScanSpeedHandler(Thing scanner, float currentSpeed);
  public delegate float SkylightTransmissionHandler(Map map, IntVec3 cell, float baseTransmission);

  /// <summary>
  /// Adjusts the colour of the light reaching <paramref name="cell"/> through skylight glass.
  /// </summary>
  /// <remarks>
  /// Handlers must <em>scale</em> <paramref name="baseTint"/> componentwise (Unity's
  /// <c>Color * Color</c>), never replace it. Consumers outside the overlay compositor rely on
  /// passing <see cref="Color.white"/> to obtain the handlers' contribution in isolation, which is
  /// also what makes an unsubscribed map provably identical to stock behaviour.
  /// The alpha channel is ignored -- this is a multiplicative RGB filter.
  /// </remarks>
  public delegate Color SkylightTintHandler(Map map, IntVec3 cell, Color baseTint);
  public delegate float SkyGlowMultiplierHandler(Map map, IntVec3 cell, float baseMultiplier);
  public delegate float SolarPowerOutputFactorHandler(Map map, IntVec3 cell, float baseFactor);
  public delegate bool DropPodRoofInterceptionHandler(Map map, IntVec3 cell, int damageAmount, ref int effectiveHitPoints);

  /// <summary>
  /// Runs the RoofBuildingPlacementCheck subscribers, returning the first decisive report or
  /// <c>null</c> to fall through to the default placement rules.
  /// </summary>
  /// <remarks>
  /// Folds global subscribers as well as per-map ones. Compatibility modules register globally
  /// (they have no per-map hook point), so a consumer that only walks the instance list never sees
  /// them at all.
  /// </remarks>
  public static AcceptanceReport? CheckRoofBuildingPlacement(BuildableDef def, IntVec3 cell, Rot4 rot, Map map)
  {
    var globalHandlers = GetGlobalHandlers<RoofBuildingPlacementCheckHandler>(HookId.RoofBuildingPlacementCheck);
    if (globalHandlers != null)
    {
      for (int i = 0; i < globalHandlers.Count; i++)
      {
        try
        {
          var result = globalHandlers[i](def, cell, rot, map);
          if (result.HasValue) return result;
        }
        catch (Exception ex)
        {
          StratumLog.Error($"Error in global RoofBuildingPlacementCheck handler: {ex}");
        }
      }
    }

    var registry = Get(map);
    if (registry != null)
    {
      var handlers = registry.GetHandlers<RoofBuildingPlacementCheckHandler>(HookId.RoofBuildingPlacementCheck);
      if (handlers != null)
      {
        for (int i = 0; i < handlers.Count; i++)
        {
          try
          {
            var result = handlers[i](def, cell, rot, map);
            if (result.HasValue) return result;
          }
          catch (Exception ex)
          {
            StratumLog.Error($"Error in RoofBuildingPlacementCheck handler: {ex}");
          }
        }
      }
    }

    return null;
  }

  /// <summary>Notifies every RoofChanged subscriber that a cell's roof changed.</summary>
  public static void NotifyRoofChanged(Map map, IntVec3 cell, RoofDef? oldRoof, RoofDef? newRoof)
  {
    var globalHandlers = GetGlobalHandlers<RoofChangedHandler>(HookId.RoofChanged);
    if (globalHandlers != null)
    {
      for (int i = 0; i < globalHandlers.Count; i++)
      {
        try
        {
          globalHandlers[i](map, cell, oldRoof, newRoof);
        }
        catch (Exception ex)
        {
          StratumLog.Error($"Error in global RoofChanged handler: {ex}");
        }
      }
    }

    var handlers = Get(map)?.GetHandlers<RoofChangedHandler>(HookId.RoofChanged);
    if (handlers != null)
    {
      for (int i = 0; i < handlers.Count; i++)
      {
        try
        {
          handlers[i](map, cell, oldRoof, newRoof);
        }
        catch (Exception ex)
        {
          StratumLog.Error($"Error in RoofChanged handler: {ex}");
        }
      }
    }
  }

  /// <summary>Runs every BeforeSetRoof subscriber, letting each adjust the roof or veto the change.</summary>
  public static void InvokeBeforeSetRoof(Map map, IntVec3 cell, RoofDef? oldRoof, ref RoofDef? newRoof, ref bool allow)
  {
    var globalHandlers = GetGlobalHandlers<BeforeSetRoofHandler>(HookId.BeforeSetRoof);
    if (globalHandlers != null)
    {
      for (int i = 0; i < globalHandlers.Count; i++)
      {
        try
        {
          globalHandlers[i](map, cell, oldRoof, ref newRoof, ref allow);
        }
        catch (Exception ex)
        {
          StratumLog.Error($"Error in global BeforeSetRoof handler: {ex}");
        }
      }
    }

    var handlers = Get(map)?.GetHandlers<BeforeSetRoofHandler>(HookId.BeforeSetRoof);
    if (handlers != null)
    {
      for (int i = 0; i < handlers.Count; i++)
      {
        try
        {
          handlers[i](map, cell, oldRoof, ref newRoof, ref allow);
        }
        catch (Exception ex)
        {
          StratumLog.Error($"Error in BeforeSetRoof handler: {ex}");
        }
      }
    }
  }

  /// <summary>Runs every PowerNetEnergyGain subscriber against the net's gain rate.</summary>
  public static void ApplyPowerNetEnergyGain(RimWorld.PowerNet net, Map map, ref float energyGainRate)
  {
    var globalHandlers = GetGlobalHandlers<PowerNetEnergyGainHandler>(HookId.PowerNetEnergyGain);
    if (globalHandlers != null)
    {
      for (int i = 0; i < globalHandlers.Count; i++)
      {
        try
        {
          globalHandlers[i](net, ref energyGainRate);
        }
        catch (Exception ex)
        {
          StratumLog.Error($"Error in global PowerNetEnergyGain handler: {ex}");
        }
      }
    }

    var handlers = Get(map)?.GetHandlers<PowerNetEnergyGainHandler>(HookId.PowerNetEnergyGain);
    if (handlers != null)
    {
      for (int i = 0; i < handlers.Count; i++)
      {
        try
        {
          handlers[i](net, ref energyGainRate);
        }
        catch (Exception ex)
        {
          StratumLog.Error($"Error in PowerNetEnergyGain handler: {ex}");
        }
      }
    }
  }

  /// <summary>
  /// Runs the RoofDamageCalculation subscribers. Returns <c>true</c> once one claims the
  /// calculation, meaning the caller should skip its own threshold/armour maths.
  /// </summary>
  public static bool TryCalculateRoofDamage(Map map, RoofDef roofDef, ThingDef? stuffDef, float baseDamage,
                                            float penetration, DamageInfo? damageInfo, ref float effectiveDamage)
  {
    var globalHandlers = GetGlobalHandlers<RoofDamageCalculationHandler>(HookId.RoofDamageCalculation);
    if (globalHandlers != null)
    {
      for (int i = 0; i < globalHandlers.Count; i++)
      {
        try
        {
          if (globalHandlers[i](roofDef, stuffDef, baseDamage, penetration, damageInfo, ref effectiveDamage)) return true;
        }
        catch (Exception ex)
        {
          StratumLog.Error($"Error in global RoofDamageCalculation handler: {ex}");
        }
      }
    }

    var handlers = Get(map)?.GetHandlers<RoofDamageCalculationHandler>(HookId.RoofDamageCalculation);
    if (handlers != null)
    {
      for (int i = 0; i < handlers.Count; i++)
      {
        try
        {
          if (handlers[i](roofDef, stuffDef, baseDamage, penetration, damageInfo, ref effectiveDamage)) return true;
        }
        catch (Exception ex)
        {
          StratumLog.Error($"Error in RoofDamageCalculation handler: {ex}");
        }
      }
    }

    return false;
  }

  /// <summary>Runs the RoofBuildingRenderCheck subscribers; first decisive answer wins.</summary>
  public static bool? CheckRoofBuildingRender(Thing thing, Map map)
  {
    var globalHandlers = GetGlobalHandlers<RoofBuildingRenderCheckHandler>(HookId.RoofBuildingRenderCheck);
    if (globalHandlers != null)
    {
      for (int i = 0; i < globalHandlers.Count; i++)
      {
        try
        {
          var result = globalHandlers[i](thing);
          if (result.HasValue) return result;
        }
        catch (Exception ex)
        {
          StratumLog.Error($"Error in global RoofBuildingRenderCheck handler: {ex}");
        }
      }
    }

    var handlers = Get(map)?.GetHandlers<RoofBuildingRenderCheckHandler>(HookId.RoofBuildingRenderCheck);
    if (handlers != null)
    {
      for (int i = 0; i < handlers.Count; i++)
      {
        try
        {
          var result = handlers[i](thing);
          if (result.HasValue) return result;
        }
        catch (Exception ex)
        {
          StratumLog.Error($"Error in RoofBuildingRenderCheck handler: {ex}");
        }
      }
    }

    return null;
  }

  /// <summary>Runs the RoofBuildingSelectableCheck subscribers; first decisive answer wins.</summary>
  public static bool? CheckRoofBuildingSelectable(Thing thing, Map map)
  {
    var globalHandlers = GetGlobalHandlers<RoofBuildingSelectableCheckHandler>(HookId.RoofBuildingSelectableCheck);
    if (globalHandlers != null)
    {
      for (int i = 0; i < globalHandlers.Count; i++)
      {
        try
        {
          var result = globalHandlers[i](thing);
          if (result.HasValue) return result;
        }
        catch (Exception ex)
        {
          StratumLog.Error($"Error in global RoofBuildingSelectableCheck handler: {ex}");
        }
      }
    }

    var handlers = Get(map)?.GetHandlers<RoofBuildingSelectableCheckHandler>(HookId.RoofBuildingSelectableCheck);
    if (handlers != null)
    {
      for (int i = 0; i < handlers.Count; i++)
      {
        try
        {
          var result = handlers[i](thing);
          if (result.HasValue) return result;
        }
        catch (Exception ex)
        {
          StratumLog.Error($"Error in RoofBuildingSelectableCheck handler: {ex}");
        }
      }
    }

    return null;
  }

  /// <summary>Runs the RoofBuildingDrawPos subscribers; first decisive answer wins.</summary>
  public static Vector3? GetRoofBuildingDrawPos(Thing thing, Vector3 originalDrawPos, Map map)
  {
    var globalHandlers = GetGlobalHandlers<RoofBuildingDrawPosHandler>(HookId.RoofBuildingDrawPos);
    if (globalHandlers != null)
    {
      for (int i = 0; i < globalHandlers.Count; i++)
      {
        try
        {
          var result = globalHandlers[i](thing, originalDrawPos);
          if (result.HasValue) return result;
        }
        catch (Exception ex)
        {
          StratumLog.Error($"Error in global RoofBuildingDrawPos handler: {ex}");
        }
      }
    }

    var handlers = Get(map)?.GetHandlers<RoofBuildingDrawPosHandler>(HookId.RoofBuildingDrawPos);
    if (handlers != null)
    {
      for (int i = 0; i < handlers.Count; i++)
      {
        try
        {
          var result = handlers[i](thing, originalDrawPos);
          if (result.HasValue) return result;
        }
        catch (Exception ex)
        {
          StratumLog.Error($"Error in RoofBuildingDrawPos handler: {ex}");
        }
      }
    }

    return null;
  }

  /// <summary>Runs the RoofBuildingTrueCenter subscribers; first decisive answer wins.</summary>
  public static Vector3? GetRoofBuildingTrueCenter(Thing thing, Vector3 originalTrueCenter, Map map)
  {
    var globalHandlers = GetGlobalHandlers<RoofBuildingTrueCenterHandler>(HookId.RoofBuildingTrueCenter);
    if (globalHandlers != null)
    {
      for (int i = 0; i < globalHandlers.Count; i++)
      {
        try
        {
          var result = globalHandlers[i](thing, originalTrueCenter);
          if (result.HasValue) return result;
        }
        catch (Exception ex)
        {
          StratumLog.Error($"Error in global RoofBuildingTrueCenter handler: {ex}");
        }
      }
    }

    var handlers = Get(map)?.GetHandlers<RoofBuildingTrueCenterHandler>(HookId.RoofBuildingTrueCenter);
    if (handlers != null)
    {
      for (int i = 0; i < handlers.Count; i++)
      {
        try
        {
          var result = handlers[i](thing, originalTrueCenter);
          if (result.HasValue) return result;
        }
        catch (Exception ex)
        {
          StratumLog.Error($"Error in RoofBuildingTrueCenter handler: {ex}");
        }
      }
    }

    return null;
  }

  /// <summary>Runs the BlocksConstruction subscribers; first decisive answer wins.</summary>
  public static bool? CheckBlocksConstruction(Thing constructible, Thing existingThing, Map map)
  {
    var globalHandlers = GetGlobalHandlers<BlocksConstructionHandler>(HookId.BlocksConstruction);
    if (globalHandlers != null)
    {
      for (int i = 0; i < globalHandlers.Count; i++)
      {
        try
        {
          var result = globalHandlers[i](constructible, existingThing);
          if (result.HasValue) return result;
        }
        catch (Exception ex)
        {
          StratumLog.Error($"Error in global BlocksConstruction handler: {ex}");
        }
      }
    }

    var handlers = Get(map)?.GetHandlers<BlocksConstructionHandler>(HookId.BlocksConstruction);
    if (handlers != null)
    {
      for (int i = 0; i < handlers.Count; i++)
      {
        try
        {
          var result = handlers[i](constructible, existingThing);
          if (result.HasValue) return result;
        }
        catch (Exception ex)
        {
          StratumLog.Error($"Error in BlocksConstruction handler: {ex}");
        }
      }
    }

    return null;
  }

  public static float GetCellSkyGlowMultiplier(Map map, IntVec3 cell, float baseMultiplier)
  {
    float current = baseMultiplier;
    var globalHandlers = GetGlobalHandlers<SkyGlowMultiplierHandler>(HookId.SkyGlowMultiplier);
    if (globalHandlers != null)
    {
      for (int i = 0; i < globalHandlers.Count; i++)
      {
        try
        {
          current = globalHandlers[i](map, cell, current);
        }
        catch (Exception ex)
        {
          StratumLog.Error($"Error in global SkyGlowMultiplier handler: {ex}");
        }
      }
    }
    var registry = Get(map);
    if (registry != null)
    {
      var handlers = registry.GetHandlers<SkyGlowMultiplierHandler>(HookId.SkyGlowMultiplier);
      if (handlers != null)
      {
        for (int i = 0; i < handlers.Count; i++)
        {
          try
          {
            current = handlers[i](map, cell, current);
          }
          catch (Exception ex)
          {
            StratumLog.Error($"Error in SkyGlowMultiplier handler: {ex}");
          }
        }
      }
    }
    return Mathf.Clamp01(current);
  }

  public static float GetCellSolarPowerOutputFactor(Map map, IntVec3 cell, float baseFactor)
  {
    float current = baseFactor;
    var globalHandlers = GetGlobalHandlers<SolarPowerOutputFactorHandler>(HookId.SolarPowerOutputFactor);
    if (globalHandlers != null)
    {
      for (int i = 0; i < globalHandlers.Count; i++)
      {
        try
        {
          current = globalHandlers[i](map, cell, current);
        }
        catch (Exception ex)
        {
          StratumLog.Error($"Error in global SolarPowerOutputFactor handler: {ex}");
        }
      }
    }
    var registry = Get(map);
    if (registry != null)
    {
      var handlers = registry.GetHandlers<SolarPowerOutputFactorHandler>(HookId.SolarPowerOutputFactor);
      if (handlers != null)
      {
        for (int i = 0; i < handlers.Count; i++)
        {
          try
          {
            current = handlers[i](map, cell, current);
          }
          catch (Exception ex)
          {
            StratumLog.Error($"Error in SolarPowerOutputFactor handler: {ex}");
          }
        }
      }
    }
    return Mathf.Clamp01(current);
  }

  public static bool InterceptDropPodByRoof(Map map, IntVec3 cell, int damageAmount, ref int effectiveHitPoints)
  {
    bool intercepted = false;
    var globalHandlers = GetGlobalHandlers<DropPodRoofInterceptionHandler>(HookId.DropPodRoofInterception);
    if (globalHandlers != null)
    {
      for (int i = 0; i < globalHandlers.Count; i++)
      {
        try
        {
          if (globalHandlers[i](map, cell, damageAmount, ref effectiveHitPoints))
          {
            intercepted = true;
          }
        }
        catch (Exception ex)
        {
          StratumLog.Error($"Error in global DropPodRoofInterception handler: {ex}");
        }
      }
    }
    var registry = Get(map);
    if (registry != null)
    {
      var handlers = registry.GetHandlers<DropPodRoofInterceptionHandler>(HookId.DropPodRoofInterception);
      if (handlers != null)
      {
        for (int i = 0; i < handlers.Count; i++)
        {
          try
          {
            if (handlers[i](map, cell, damageAmount, ref effectiveHitPoints))
            {
              intercepted = true;
            }
          }
          catch (Exception ex)
          {
            StratumLog.Error($"Error in DropPodRoofInterception handler: {ex}");
          }
        }
      }
    }
    return intercepted;
  }
}
