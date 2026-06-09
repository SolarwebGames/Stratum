using System.Collections.Generic;
using Verse;

using SolarWeb.Stratum.DefModExtensions;

namespace SolarWeb.Stratum.AI;

public static class RoofMaterialUtils
{
  public static bool HasAllMaterialsOnFloor(IntVec3 cell, Map map, BuildableRoofExtension ext)
  {
    var bDef = ext.buildableDef;
    if (bDef == null || bDef.CostList.NullOrEmpty()) return true;

    foreach (var cost in bDef.CostList)
    {
      int totalNeeded = cost.count;
      int found = 0;

      foreach (var t in map.thingGrid.ThingsAt(cell))
      {
        if (t.def == cost.thingDef) found += t.stackCount;
      }

      if (found < totalNeeded) return false;
    }
    return true;
  }

  public static void ConsumeMaterialsAt(IntVec3 cell, Map map, BuildableRoofExtension ext)
  {
    var bDef = ext.buildableDef;
    if (bDef == null || bDef.CostList.NullOrEmpty()) return;
    foreach (var cost in bDef.CostList)
    {
      int needed = cost.count;
      var things = new List<Thing>(map.thingGrid.ThingsAt(cell));
      foreach (var t in things)
      {
        if (t.def == cost.thingDef)
        {
          int toTake = UnityEngine.Mathf.Min(needed, t.stackCount);
          t.SplitOff(toTake).Destroy();
          needed -= toTake;
          if (needed <= 0) break;
        }
      }
    }
  }
}
