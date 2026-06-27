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

  [DefAlias($"{DefOfConstants.DefAliasPrefix}RetractableRoofConsole")]
  public static ThingDef RetractableRoofConsole = default!;

  [DefAlias($"{DefOfConstants.DefAliasPrefix}LightningRod")]
  public static ThingDef LightningRod = default!;


  static ThingDefOf()
  {
    DefOfHelper.EnsureInitializedInCtor(typeof(ThingDefOf));
  }
}
