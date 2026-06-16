using HarmonyLib;
using SaveOurShip2;
using Verse;

using SolarWeb.Stratum.Stats;

namespace SolarWeb.Stratum.Patches.SOS2;

[StaticConstructorOnStartup]
public static class SOS2Compatibility
{
  static SOS2Compatibility()
  {
    var harmony = new Harmony("com.solarweb.Stratum.SOS2");

    var isAirtightMethod = AccessTools.Method(typeof(ShipInteriorMod2), "IsRoofDefAirtight");
    if (isAirtightMethod != null)
    {
      harmony.Patch(isAirtightMethod, postfix: new HarmonyMethod(typeof(SOS2Compatibility), nameof(IsRoofDefAirtight_Postfix)));
    }

    var solarProps = AccessTools.Property(typeof(CompPowerPlantSolarShip), "RoofedPowerOutputFactor");
    if (solarProps?.GetMethod != null)
    {
      harmony.Patch(solarProps.GetMethod, prefix: new HarmonyMethod(typeof(SOS2Compatibility), nameof(SolarShip_RoofedPowerOutputFactor_Prefix)));
    }

    var spaceRoomCheck = AccessTools.Method("SaveOurShip2.HarmonyPatches+SpaceRoomCheck:Postfix");
    if (spaceRoomCheck != null)
    {
      harmony.Patch(spaceRoomCheck, postfix: new HarmonyMethod(typeof(SOS2Compatibility), nameof(SpaceRoomCheck_Postfix)));
    }
  }

  public static void IsRoofDefAirtight_Postfix(RoofDef roof, ref bool __result)
  {
    if (!__result && RoofStatCache.IsCustomRoof(roof))
    {
      // We can only check base airtightness here as we lack cell context.
      // Cell-specific airtightness (stuff-based) is handled in SpaceRoomCheck_Postfix.
      __result = RoofStatCache.GetIsAirtight(roof);
    }
  }

  public static void SpaceRoomCheck_Postfix(Room __instance, ref int ___cachedOpenRoofCount)
  {
    if (___cachedOpenRoofCount != 0) return;

    var map = __instance.Map;
    if (map == null || !map.IsSpace()) return;

    var integrity = map.GetComponent<MapComponents.RoofIntegrityGrid>();

    foreach (var cell in __instance.Cells)
    {
      var roof = map.roofGrid.RoofAt(cell);
      if (roof != null && RoofStatCache.IsCustomRoof(roof))
      {
        var stuff = integrity?.GetStuff(cell);
        if (!RoofStatCache.GetIsAirtight(roof, stuff))
        {
          ___cachedOpenRoofCount = 1;
          return;
        }
      }
    }
  }

  public static bool SolarShip_RoofedPowerOutputFactor_Prefix(CompPowerPlantSolarShip __instance, ref float __result)
  {
    if (__instance.unfoldTo == null) return true;

    int total = 0;
    float passage = 0f;
    var map = __instance.parent.Map;

    foreach (var c in __instance.unfoldTo)
    {
      total++;
      var roof = map.roofGrid.RoofAt(c);
      if (roof == null)
      {
        passage += 1f;
      }
      else
      {
        if (RoofStatCache.IsCustomRoof(roof))
        {
          passage += RoofStatCache.GetTransparency(roof);
        }
        else
        {
          if (!map.roofGrid.Roofed(c))
          {
            passage += 1f;
          }
        }
      }
    }

    __result = total > 0 ? passage / total : 1f;
    return false;
  }
}
