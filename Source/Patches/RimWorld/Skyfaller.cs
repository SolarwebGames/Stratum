using HarmonyLib;
using RimWorld;
using SolarWeb.Stratum.DefModExtensions;
using SolarWeb.Stratum.MapComponents;
using Verse;

namespace SolarWeb.Stratum.Patches;

[HarmonyPatch]
public static class Skyfaller_Patch
{
  [HarmonyPatch(typeof(Skyfaller), "Impact")]
  [HarmonyPrefix]
  public static bool Impact_Prefix(Skyfaller __instance)
  {
    var map = __instance.Map;
    if (map == null) return true;

    var pos = __instance.Position;
    var roof = map.roofGrid.RoofAt(pos);

    if (roof != null && roof.HasModExtension<BuildableRoofExtension>())
    {
      var integrityGrid = map.GetComponent<RoofIntegrityGrid>();
      if (integrityGrid != null)
      {
        var skyfallerHealth = (__instance.def.BaseMaxHitPoints < 300) ? __instance.def.BaseMaxHitPoints : 300;

        integrityGrid.TakeDamage(pos, skyfallerHealth);

        short hpAfter = integrityGrid.GetHitPoints(pos);

        if (hpAfter > 0)
        {
          for (int i = 0; i < 6; i++)
          {
            FleckMaker.ThrowDustPuff(pos.ToVector3Shifted() + Gen.RandomHorizontalVector(1f), map, 1.2f);
          }
          FleckMaker.ThrowLightningGlow(pos.ToVector3Shifted(), map, 2f);
          GenClamor.DoClamor(__instance, 15f, ClamorDefOf.Impact);

          __instance.Destroy();

          return false;
        }
        else
        {
          return true;
        }
      }
    }

    return true;
  }
}
