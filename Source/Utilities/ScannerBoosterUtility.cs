using System.Collections.Generic;
using RimWorld;
using Verse;

using SolarWeb.Stratum.Hooks;
using SolarWeb.Stratum.ThingComps;

namespace SolarWeb.Stratum.Utilities;

[StaticConstructorOnStartup]
public static class ScannerBoosterUtility
{
  private static readonly HashSet<ScannerBooster> activeBoosters = [];

  static ScannerBoosterUtility()
  {
    MapHookRegistry.RegisterGlobal<MapHookRegistry.ScanSpeedHandler>(
      MapHookRegistry.HookId.ScanSpeed,
      ApplyBoosterOffset);
  }

  private static float ApplyBoosterOffset(Thing scanner, float currentSpeed)
  {
    if (scanner == null) return currentSpeed;

    var affectedByFacilities = scanner.TryGetComp<CompAffectedByFacilities>();
    if (affectedByFacilities != null)
    {
      var linked = affectedByFacilities.LinkedFacilitiesListForReading;
      for (int i = 0; i < linked.Count; i++)
      {
        if (linked[i].TryGetComp<ScannerBooster>() != null)
        {
          return currentSpeed;
        }
      }
    }

    var map = scanner.MapHeld ?? scanner.Map;
    float offset = GetBoosterOffset(map, scanner.Position);
    return currentSpeed + offset;
  }

  public static void Register(ScannerBooster booster)
  {
    if (booster != null)
    {
      activeBoosters.Add(booster);
    }
  }

  public static void Deregister(ScannerBooster booster)
  {
    if (booster != null)
    {
      activeBoosters.Remove(booster);
    }
  }

  public static float GetBoosterOffset(Map? map, IntVec3 origin)
  {
    if (map == null) return 0f;

    float maxOffset = 0f;
    foreach (var booster in activeBoosters)
    {
      if (booster.parent != null && booster.parent.Spawned && booster.parent.Map == map)
      {
        float offset = booster.GetBoosterOffset(origin);
        if (offset > maxOffset)
        {
          maxOffset = offset;
        }
      }
    }
    return maxOffset;
  }

  public static float GetBoosterMultiplier(Map? map, IntVec3 origin) => 1f + GetBoosterOffset(map, origin);

  public static float GetScanSpeed(Thing scanner, float baseSpeed = 1f) =>
    MapHookRegistry.GetScanSpeed(scanner, baseSpeed);
}
