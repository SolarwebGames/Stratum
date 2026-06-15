using RimWorld;
using Verse;

namespace SolarWeb.Stratum.DefOf;

[RimWorld.DefOf]
public static class FleckDefOf
{
  [DefAlias($"{DefOfConstants.DefAliasPrefix}Sunbeam")]
  public static FleckDef Sunbeam = default!;

  [DefAlias($"{DefOfConstants.DefAliasPrefix}RoofExplosionFlash")]
  public static FleckDef RoofExplosionFlash = default!;

  [DefAlias($"{DefOfConstants.DefAliasPrefix}RoofSmoke")]
  public static FleckDef RoofSmoke = default!;

  [DefAlias($"{DefOfConstants.DefAliasPrefix}RoofSparks")]
  public static FleckDef RoofSparks = default!;

  static FleckDefOf()
  {
    DefOfHelper.EnsureInitializedInCtor(typeof(FleckDefOf));
  }
}
