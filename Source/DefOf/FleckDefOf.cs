using RimWorld;
using Verse;

namespace SolarWeb.Stratum.DefOf;

[RimWorld.DefOf]
public static class FleckDefOf
{
  [DefAlias($"{DefOfConstants.DefAliasPrefix}Sunbeam")]
  public static FleckDef Sunbeam = default!;

  static FleckDefOf()
  {
    DefOfHelper.EnsureInitializedInCtor(typeof(FleckDefOf));
  }
}
