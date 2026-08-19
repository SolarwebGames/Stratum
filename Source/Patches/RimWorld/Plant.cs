using HarmonyLib;
using RimWorld;
using Verse;
using SolarWeb.Stratum.ThingComps;
using SolarWeb.Stratum.MapComponents;

namespace SolarWeb.Stratum.Patches;

[HarmonyPatch(typeof(Plant))]
public static class Plant_Patch
{
  [HarmonyPatch(nameof(Plant.GrowthRate), MethodType.Getter)]
  [HarmonyPostfix]
  public static void GrowthRate_Postfix(Plant __instance, ref float __result)
  {
    if (__instance.Map == null || !__instance.Spawned) return;

    var tracker = __instance.Map.GetComponent<GrowthBoosterTracker>();
    if (tracker == null || tracker.boosters.Count == 0) return;

    var plantPos = __instance.Position;
    Room? room = null;
    float bestFactor = 1f;

    foreach (var booster in tracker.boosters)
    {
      if (booster.IsActive && booster.Props != null)
      {
        var props = booster.Props;
        if (props.growthRateFactor <= bestFactor) continue;

        var boosterPos = booster.parent.Position;
        float radius = props.radius;
        int dx = plantPos.x - boosterPos.x;
        int dz = plantPos.z - boosterPos.z;
        if (dx * dx + dz * dz > radius * radius) continue;

        room ??= __instance.GetRoom();
        if (room == null) return;

        if (!props.roomRestricted || booster.parent.GetRoom() == room)
        {
          bestFactor = props.growthRateFactor;
        }
      }
    }

    __result *= bestFactor;
  }

  [HarmonyPatch(nameof(Plant.GrowthRateCalcDesc), MethodType.Getter)]
  [HarmonyPostfix]
  public static void GrowthRateCalcDesc_Postfix(Plant __instance, ref string __result)
  {
    if (__instance.Map == null || !__instance.Spawned) return;

    var tracker = __instance.Map.GetComponent<GrowthBoosterTracker>();
    if (tracker == null || tracker.boosters.Count == 0) return;

    var plantPos = __instance.Position;
    Room? room = null;
    GrowthBooster? bestBooster = null;
    float bestFactor = 1f;

    foreach (var booster in tracker.boosters)
    {
      if (booster.IsActive && booster.Props != null)
      {
        var props = booster.Props;
        if (props.growthRateFactor <= bestFactor) continue;

        var boosterPos = booster.parent.Position;
        float radius = props.radius;
        int dx = plantPos.x - boosterPos.x;
        int dz = plantPos.z - boosterPos.z;
        if (dx * dx + dz * dz > radius * radius) continue;

        room ??= __instance.GetRoom();
        if (room == null) return;

        if (!props.roomRestricted || booster.parent.GetRoom() == room)
        {
          bestFactor = props.growthRateFactor;
          bestBooster = booster;
        }
      }
    }

    if (bestBooster != null)
    {
      string label = bestBooster.parent.LabelCap;
      float percentage = (bestFactor - 1f) * 100f;
      string entry = $"{label}: +{percentage:F0}%";

      if (string.IsNullOrEmpty(__result))
      {
        __result = entry;
      }
      else
      {
        __result += $"\n{entry}";
      }
    }
  }
}
