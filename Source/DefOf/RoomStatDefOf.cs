using RimWorld;
using Verse;

namespace SolarWeb.Stratum.DefOf;

[RimWorld.DefOf]
public static class RoomStatDefOf
{
  [DefAlias($"{DefOfConstants.DefAliasPrefix}SkylightPercentage")]
  public static RoomStatDef SkylightPercentage = default!;

  static RoomStatDefOf()
  {
    DefOfHelper.EnsureInitializedInCtor(typeof(RoomStatDefOf));
  }
}
