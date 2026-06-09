using RimWorld;
using Verse;

namespace SolarWeb.Stratum.DefOf;

[RimWorld.DefOf]
public static class DesignationDefOf
{
  [DefAlias($"{DefOfConstants.DefAliasPrefix}BuildCustomRoof")]
  public static DesignationDef BuildCustomRoof = default!;

  static DesignationDefOf()
  {
    DefOfHelper.EnsureInitializedInCtor(typeof(DesignationDefOf));
  }
}
