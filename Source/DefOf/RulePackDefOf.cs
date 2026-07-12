using RimWorld;
using Verse;

namespace SolarWeb.Stratum.DefOf;

[RimWorld.DefOf]
public static class RulePackDefOf
{
  [DefAlias($"{DefOfConstants.DefAliasPrefix}PA_Calming_RulePack")]
  public static RulePackDef PA_Calming_RulePack = default!;

  [DefAlias($"{DefOfConstants.DefAliasPrefix}PA_Inspirational_RulePack")]
  public static RulePackDef PA_Inspirational_RulePack = default!;

  [DefAlias($"{DefOfConstants.DefAliasPrefix}PA_Educational_RulePack")]
  public static RulePackDef PA_Educational_RulePack = default!;

  [DefAlias($"{DefOfConstants.DefAliasPrefix}PA_Analytical_RulePack")]
  public static RulePackDef PA_Analytical_RulePack = default!;

  [DefAlias($"{DefOfConstants.DefAliasPrefix}PA_SleepAid_RulePack")]
  public static RulePackDef PA_SleepAid_RulePack = default!;

  static RulePackDefOf()
  {
    DefOfHelper.EnsureInitializedInCtor(typeof(RulePackDefOf));
  }
}
