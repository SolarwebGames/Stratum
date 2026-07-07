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

  [DefAlias($"{DefOfConstants.DefAliasPrefix}OperateRetractableRoofConsole")]
  public static JobDef OperateRetractableRoofConsole = default!;

  [DefAlias($"{DefOfConstants.DefAliasPrefix}SmoothRoof")]
  public static JobDef SmoothRoof = default!;

  static JobDefOf()
  {
    DefOfHelper.EnsureInitializedInCtor(typeof(JobDefOf));
  }
}
