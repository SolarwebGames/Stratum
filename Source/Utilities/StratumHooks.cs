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
}
