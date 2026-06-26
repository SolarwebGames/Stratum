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

  public static ThingDef Filth_Water = default!;

  public static ThingDef LightningRod_Rooftop = default!;

  public static ThingDef ChunkSandstone = default!;

  public static ThingDef ChunkGranite = default!;

  public static ThingDef ChunkLimestone = default!;

  public static ThingDef ChunkSlate = default!;

  public static ThingDef ChunkMarble = default!;

  static ThingDefOf()
  {
    DefOfHelper.EnsureInitializedInCtor(typeof(ThingDefOf));
  }
}
