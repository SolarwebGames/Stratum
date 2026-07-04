using RimWorld;
using Verse;

namespace SolarWeb.Stratum.DefOf;

[RimWorld.DefOf]
public static class RoomRoleDefOf
{
  [DefAlias($"{DefOfConstants.DefAliasPrefix}Greenhouse")]
  public static RoomRoleDef Greenhouse = default!;

  static RoomRoleDefOf()
  {
    DefOfHelper.EnsureInitializedInCtor(typeof(RoomRoleDefOf));
  }
}
