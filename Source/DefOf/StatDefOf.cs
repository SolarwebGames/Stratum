using RimWorld;

namespace SolarWeb.Stratum.DefOf;

[RimWorld.DefOf]
public static class StatDefOf
{
  [DefAlias($"{DefOfConstants.DefAliasPrefix}Insulation")]
  public static RimWorld.StatDef Insulation = default!;

  [DefAlias($"{DefOfConstants.DefAliasPrefix}Transparency")]
  public static RimWorld.StatDef Transparency = default!;

  [DefAlias($"{DefOfConstants.DefAliasPrefix}SolarOutput")]
  public static RimWorld.StatDef SolarOutput = default!;

  [DefAlias($"{DefOfConstants.DefAliasPrefix}ThermalConductivity")]
  public static RimWorld.StatDef ThermalConductivity = default!;

  [DefAlias($"{DefOfConstants.DefAliasPrefix}StuffInsulationMultiplier")]
  public static RimWorld.StatDef StuffInsulationMultiplier = default!;

  [DefAlias($"{DefOfConstants.DefAliasPrefix}DamageThreshold")]
  public static RimWorld.StatDef DamageThreshold = default!;

  [DefAlias($"{DefOfConstants.DefAliasPrefix}ArmorRating")]
  public static RimWorld.StatDef ArmorRating = default!;

  [DefAlias($"{DefOfConstants.DefAliasPrefix}StuffArmorMultiplier")]
  public static RimWorld.StatDef StuffArmorMultiplier = default!;

  [DefAlias($"{DefOfConstants.DefAliasPrefix}TransitionSpeed")]
  public static RimWorld.StatDef TransitionSpeed = default!;

  [DefAlias($"{DefOfConstants.DefAliasPrefix}StuffBeautyMultiplier")]
  public static RimWorld.StatDef StuffBeautyMultiplier = default!;

  [DefAlias($"{DefOfConstants.DefAliasPrefix}ScanSpeed")]
  public static RimWorld.StatDef ScanSpeed = default!;

  [DefAlias($"{DefOfConstants.DefAliasPrefix}ScanSpeedOffset")]
  public static RimWorld.StatDef ScanSpeedOffset = default!;

  static StatDefOf()
  {
    DefOfHelper.EnsureInitializedInCtor(typeof(StatDefOf));
  }
}
