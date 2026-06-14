using RimWorld;

namespace SolarWeb.Stratum.DefOf;

[RimWorld.DefOf]
public static class StatDefOf
{
  [DefAlias($"{DefOfConstants.DefAliasPrefix}Insulation")]
  public static RimWorld.StatDef Insulation = default!;

  static StatDefOf()
  {
    DefOfHelper.EnsureInitializedInCtor(typeof(StatDefOf));
  }
}
