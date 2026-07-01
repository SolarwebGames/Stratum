using RimWorld;
using Verse;

namespace SolarWeb.Stratum.DefOf;

[RimWorld.DefOf]
public static class HediffDefOf
{
  [DefAlias($"{DefOfConstants.DefAliasPrefix}PA_Calming_Hediff")]
  public static HediffDef PA_Calming_Hediff = default!;

  [DefAlias($"{DefOfConstants.DefAliasPrefix}PA_Inspirational_Hediff")]
  public static HediffDef PA_Inspirational_Hediff = default!;

  [DefAlias($"{DefOfConstants.DefAliasPrefix}PA_Educational_Hediff")]
  public static HediffDef PA_Educational_Hediff = default!;

  [DefAlias($"{DefOfConstants.DefAliasPrefix}PA_Analytical_Hediff")]
  public static HediffDef PA_Analytical_Hediff = default!;

  [DefAlias($"{DefOfConstants.DefAliasPrefix}PA_SleepAid_Hediff")]
  public static HediffDef PA_SleepAid_Hediff = default!;

  [DefAlias($"{DefOfConstants.DefAliasPrefix}CeilingFanBreeze")]
  public static HediffDef CeilingFanBreeze = default!;

  static HediffDefOf()
  {
    DefOfHelper.EnsureInitializedInCtor(typeof(HediffDefOf));
  }
}
