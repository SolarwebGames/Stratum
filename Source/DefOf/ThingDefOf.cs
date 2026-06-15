using RimWorld;
using Verse;

namespace SolarWeb.Stratum.DefOf;

[RimWorld.DefOf]
public static class ThingDefOf
{
  [DefAlias($"{DefOfConstants.DefAliasPrefix}RoofFrame")]
  public static ThingDef RoofFrame = default!;

  [DefAlias($"{DefOfConstants.DefAliasPrefix}RoofFire")]
  public static ThingDef RoofFire = default!;

  [DefAlias($"{DefOfConstants.DefAliasPrefix}RoofExplosion")]
  public static ThingDef RoofExplosion = default!;

  static ThingDefOf()
  {
    DefOfHelper.EnsureInitializedInCtor(typeof(ThingDefOf));
  }
}
