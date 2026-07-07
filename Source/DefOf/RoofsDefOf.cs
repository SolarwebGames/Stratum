using RimWorld;
using SolarWeb.Stratum.Defs;

namespace SolarWeb.Stratum.DefOf;

[RimWorld.DefOf]
public static class RoofDefOf
{
  [DefAlias($"{DefOfConstants.DefAliasPrefix}HullPlating")]
  public static StratumRoofDef HullPlating = default!;

  [DefAlias($"{DefOfConstants.DefAliasPrefix}ReinforcedHullPlating")]
  public static StratumRoofDef ReinforcedHullPlating = default!;

  [DefAlias($"{DefOfConstants.DefAliasPrefix}SolarHullPlate")]
  public static StratumRoofDef SolarHullPlate = default!;

  [DefAlias($"{DefOfConstants.DefAliasPrefix}ReinforcedGlassPanel")]
  public static StratumRoofDef ReinforcedGlassPanel = default!;

  [DefAlias($"{DefOfConstants.DefAliasPrefix}RoofThatch")]
  public static StratumRoofDef RoofThatch = default!;

  [DefAlias($"{DefOfConstants.DefAliasPrefix}RoofThatchHay")]
  public static StratumRoofDef RoofThatchHay = default!;

  [DefAlias($"{DefOfConstants.DefAliasPrefix}RoofGlass")]
  public static StratumRoofDef RoofGlass = default!;

  [DefAlias($"{DefOfConstants.DefAliasPrefix}RoofSolarShingles")]
  public static StratumRoofDef RoofSolarShingles = default!;

  [DefAlias($"{DefOfConstants.DefAliasPrefix}RoofThinRockSmoothed")]
  public static StratumRoofDef RoofThinRockSmoothed = default!;

  [DefAlias($"{DefOfConstants.DefAliasPrefix}RoofOverheadMountainSmoothed")]
  public static StratumRoofDef RoofOverheadMountainSmoothed = default!;

  static RoofDefOf()
  {
    DefOfHelper.EnsureInitializedInCtor(typeof(RoofDefOf));
  }
}
