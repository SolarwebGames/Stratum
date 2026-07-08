using System;
using Verse;

namespace SolarWeb.Stratum.Utilities;

/// <summary>
/// Provides static hooks and delegates for other mods to integrate with Stratum without direct dependencies
/// or excessive Harmony patching.
/// </summary>
public static class StratumHooks
{
  /// <summary>
  /// Called after a roof is changed on a map.
  /// (Map, Cell, OldRoof, NewRoof)
  /// </summary>
  public static Action<Map, IntVec3, RoofDef?, RoofDef?>? OnRoofChanged;

  /// <summary>
  /// Allows other mods to override the airtightness of a roof.
  /// If any delegate returns true, the roof is considered airtight.
  /// </summary>
  public static Func<RoofDef, bool>? GlobalAirtightCheck;

  /// <summary>
  /// Allows other mods to provide a transparency value for a roof (0.0 to 1.0).
  /// The highest value returned by any delegate will be used.
  /// </summary>
  public static Func<RoofDef, float>? GlobalTransparencyCheck;

  /// <summary>
  /// Allows other mods to dynamically override or modify the thermal conductivity of a specific cell.
  /// (Map, Cell, BaseConductivity) -> ModifiedConductivity
  /// </summary>
  public static Func<Map, IntVec3, float, float>? GetCellThermalConductivity;

  /// <summary>
  /// Allows other mods to dynamically override or modify the max hit points of a specific roof cell.
  /// (Map, Cell, BaseMaxHP) -> ModifiedMaxHP
  /// </summary>
  public static Func<Map, IntVec3, int, int>? GetCellRoofMaxHitPoints;

  /// <summary>
  /// Allows other mods to dynamically override or modify the damage threshold of a specific roof cell.
  /// (Map, Cell, BaseDT) -> ModifiedDT
  /// </summary>
  public static Func<Map, IntVec3, float, float>? GetCellRoofDamageThreshold;

  /// <summary>
  /// Allows other mods to dynamically override or modify the armor rating of a specific roof cell.
  /// (Map, Cell, BaseArmor) -> ModifiedArmor
  /// </summary>
  public static Func<Map, IntVec3, float, float>? GetCellRoofArmorRating;

  public delegate bool RoofDamageCalculationHandler(RoofDef roof, ThingDef? stuff, float amount, float penetration, DamageInfo? dinfo, ref float effectiveDamage);

  /// <summary>
  /// Allows other mods to override damage calculations for a roof.
  /// Returns true if handled, false to fallback to default calculation.
  /// </summary>
  public static RoofDamageCalculationHandler? OnCalculateDamage;

  public delegate void PowerNetEnergyGainHandler(RimWorld.PowerNet net, ref float energyGainRate);

  /// <summary>
  /// Called after vanilla PowerNet.CurrentEnergyGainRate calculation.
  /// Allows external components to safely add or subtract from the net's energy gain.
  /// </summary>
  public static PowerNetEnergyGainHandler? OnCalculateEnergyGainRate;

  internal static void Notify_RoofChanged(Map map, IntVec3 c, RoofDef? oldRoof, RoofDef? newRoof)
  {
    OnRoofChanged?.Invoke(map, c, oldRoof, newRoof);
  }

  internal static bool IsAirtightOverride(RoofDef def)
  {
    if (GlobalAirtightCheck == null) return false;
    foreach (Func<RoofDef, bool> check in GlobalAirtightCheck.GetInvocationList())
    {
      if (check(def)) return true;
    }
    return false;
  }

  internal static float GetTransparencyOverride(RoofDef def)
  {
    if (GlobalTransparencyCheck == null) return 0f;
    float max = 0f;
    foreach (Func<RoofDef, float> check in GlobalTransparencyCheck.GetInvocationList())
    {
      max = Math.Max(max, check(def));
    }
    return max;
  }

  internal static float GetCellThermalConductivityOverride(Map map, IntVec3 c, float baseConductivity)
  {
    if (GetCellThermalConductivity == null) return baseConductivity;
    float current = baseConductivity;
    foreach (Func<Map, IntVec3, float, float> check in GetCellThermalConductivity.GetInvocationList())
    {
      current = check(map, c, current);
    }
    return current;
  }

  internal static int GetCellRoofMaxHitPointsOverride(Map map, IntVec3 c, int baseMaxHp)
  {
    if (GetCellRoofMaxHitPoints == null) return baseMaxHp;
    int current = baseMaxHp;
    foreach (Func<Map, IntVec3, int, int> check in GetCellRoofMaxHitPoints.GetInvocationList())
    {
      current = check(map, c, current);
    }
    return current;
  }

  internal static float GetCellRoofDamageThresholdOverride(Map map, IntVec3 c, float baseDt)
  {
    if (GetCellRoofDamageThreshold == null) return baseDt;
    float current = baseDt;
    foreach (Func<Map, IntVec3, float, float> check in GetCellRoofDamageThreshold.GetInvocationList())
    {
      current = check(map, c, current);
    }
    return current;
  }

  internal static float GetCellRoofArmorRatingOverride(Map map, IntVec3 c, float baseArmor)
  {
    if (GetCellRoofArmorRating == null) return baseArmor;
    float current = baseArmor;
    foreach (Func<Map, IntVec3, float, float> check in GetCellRoofArmorRating.GetInvocationList())
    {
      current = check(map, c, current);
    }
    return current;
  }
}
