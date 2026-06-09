using RimWorld;
using Verse;

namespace SolarWeb.Stratum.DefOf;

[RimWorld.DefOf]
public static class ThingDefOf
{
  [DefAlias($"{DefOfConstants.DefAliasPrefix}RoofFrame")]
  public static ThingDef RoofFrame = default!;

  static ThingDefOf()
  {
    DefOfHelper.EnsureInitializedInCtor(typeof(ThingDefOf));
  }
}
