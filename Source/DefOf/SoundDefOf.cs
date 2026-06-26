using RimWorld;
using Verse;

namespace SolarWeb.Stratum.DefOf;

[RimWorld.DefOf]
public static class SoundDefOf
{
  public static SoundDef DropPod_Open = default!;

  static SoundDefOf()
  {
    DefOfHelper.EnsureInitializedInCtor(typeof(SoundDefOf));
  }
}
