using RimWorld;
using Verse;

namespace SolarWeb.Stratum.DefOf;

[RimWorld.DefOf]
public static class DesignationDefOf
{
  [DefAlias($"{DefOfConstants.DefAliasPrefix}BuildCustomRoof")]
  public static DesignationDef BuildCustomRoof = default!;

  [DefAlias($"{DefOfConstants.DefAliasPrefix}SmoothRoof")]
  public static DesignationDef SmoothRoof = default!;

  static DesignationDefOf()
  {
    DefOfHelper.EnsureInitializedInCtor(typeof(DesignationDefOf));
  }
}
