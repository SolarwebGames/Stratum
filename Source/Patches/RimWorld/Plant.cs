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

    Room room = __instance.GetRoom();
    if (room == null) return;

    float bestFactor = 1f;

    foreach (var booster in tracker.boosters)
    {
      if (booster.IsActive && booster.Props != null)
      {
        if (!booster.Props.roomRestricted || booster.parent.GetRoom() == room)
        {
          if (__instance.Position.DistanceTo(booster.parent.Position) <= booster.Props.radius)
          {
            if (booster.Props.growthRateFactor > bestFactor)
            {
              bestFactor = booster.Props.growthRateFactor;
            }
          }
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

    Room room = __instance.GetRoom();
    if (room == null) return;

    GrowthBooster? bestBooster = null;
    float bestFactor = 1f;

    foreach (var booster in tracker.boosters)
    {
      if (booster.IsActive && booster.Props != null)
      {
        if (!booster.Props.roomRestricted || booster.parent.GetRoom() == room)
        {
          if (__instance.Position.DistanceTo(booster.parent.Position) <= booster.Props.radius)
          {
            if (booster.Props.growthRateFactor > bestFactor)
            {
              bestFactor = booster.Props.growthRateFactor;
              bestBooster = booster;
            }
          }
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
