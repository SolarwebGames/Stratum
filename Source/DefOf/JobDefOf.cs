using RimWorld;
using Verse;

namespace SolarWeb.Stratum.DefOf;

[RimWorld.DefOf]
public static class JobDefOf
{
  [DefAlias($"{DefOfConstants.DefAliasPrefix}DeliverRoofIngredients")]
  public static JobDef DeliverRoofIngredients = default!;

  [DefAlias($"{DefOfConstants.DefAliasPrefix}BuildCustomRoof")]
  public static JobDef BuildCustomRoof = default!;

  [DefAlias($"{DefOfConstants.DefAliasPrefix}RepairCustomRoof")]
  public static JobDef RepairCustomRoof = default!;

  static JobDefOf()
  {
    DefOfHelper.EnsureInitializedInCtor(typeof(JobDefOf));
  }
}
