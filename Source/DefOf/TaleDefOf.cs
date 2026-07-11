using RimWorld;

namespace SolarWeb.Stratum.DefOf;

[RimWorld.DefOf]
public static class TaleDefOf
{
  [DefAlias($"{DefOfConstants.DefAliasPrefix}GreenhouseGazedTogether")]
  public static TaleDef GreenhouseGazedTogether = default!;



  static TaleDefOf()
  {
    DefOfHelper.EnsureInitializedInCtor(typeof(TaleDefOf));
  }
}
