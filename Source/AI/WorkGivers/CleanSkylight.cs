using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

using SolarWeb.Stratum.MapComponents;

namespace SolarWeb.Stratum.AI.WorkGivers;

public class CleanSkylight : WorkGiver_Scanner
{
  public override PathEndMode PathEndMode => PathEndMode.Touch;

  public override Danger MaxPathDanger(Pawn pawn)
  {
    return Danger.Deadly;
  }

  public override IEnumerable<IntVec3> PotentialWorkCellsGlobal(Pawn pawn)
  {
    var map = pawn.Map;
    if (map == null) yield break;

    // Log information about the cell currently under the player's mouse
    IntVec3 mouseCell = Verse.UI.MouseCell();
    if (mouseCell.InBounds(map))
    {
      var r = map.roofGrid.RoofAt(mouseCell);
      var coating = map.GetComponent<SkylightCoating>();
      float dVal = coating != null ? (coating.GetDirtLevel(mouseCell) + coating.GetPollenLevel(mouseCell) + coating.GetSnowLevel(mouseCell)) : 0f;
      bool isHome = map.areaManager.Home[mouseCell];
      bool isSkylight = r != null && Stats.RoofStatCache.IsSkylight(r);
      
      Log.Message($"[Stratum Debug] PotentialWorkCellsGlobal: mouseCell={mouseCell}, isHome={isHome}, roof={r?.defName ?? "null"}, isSkylight={isSkylight}, dirtTotal={dVal:F4}, rain={map.weatherManager.RainRate:F2}, snow={map.weatherManager.SnowRate:F2}");
    }

    if (map.weatherManager.RainRate > 0.1f || map.weatherManager.SnowRate > 0.1f) yield break;

    var dirt = map.GetComponent<SkylightCoating>();
    if (dirt == null) yield break;

    foreach (var cell in map.areaManager.Home.ActiveCells)
    {
      var roof = map.roofGrid.RoofAt(cell);
      if (roof != null && Stats.RoofStatCache.IsSkylight(roof))
      {
        if (dirt.GetDirtLevel(cell) + dirt.GetPollenLevel(cell) + dirt.GetSnowLevel(cell) > 0.2f)
        {
          yield return cell;
        }
      }
    }
  }

  public override bool HasJobOnCell(Pawn pawn, IntVec3 cell, bool forced = false)
  {
    var map = pawn.Map;
    if (map == null) return false;

    if (pawn.Faction == Faction.OfPlayer && !map.areaManager.Home[cell] && !forced)
    {
      JobFailReason.Is("NotInHomeArea".Translate());
      return false;
    }

    if (!forced && (map.weatherManager.RainRate > 0.1f || map.weatherManager.SnowRate > 0.1f))
    {
      return false;
    }

    var dirt = map.GetComponent<SkylightCoating>();
    if (dirt == null)
    {
      Log.Warning($"[Stratum Debug] HasJobOnCell for {cell}: SkylightCoating component is null!");
      return false;
    }

    float totalDirt = dirt.GetDirtLevel(cell) + dirt.GetPollenLevel(cell) + dirt.GetSnowLevel(cell);
    if (totalDirt <= 0.2f)
    {
      Log.Message($"[Stratum Debug] HasJobOnCell for {cell}: Dirt level {totalDirt} is below threshold 0.2");
      return false;
    }

    var roof = map.roofGrid.RoofAt(cell);
    if (roof == null)
    {
      Log.Message($"[Stratum Debug] HasJobOnCell for {cell}: Roof is null!");
      return false;
    }

    if (!Stats.RoofStatCache.IsSkylight(roof))
    {
      Log.Message($"[Stratum Debug] HasJobOnCell for {cell}: Roof '{roof.defName}' is not recognized as a skylight!");
      return false;
    }

    if (!pawn.CanReserve(cell, 1, -1, null, forced))
    {
      Log.Message($"[Stratum Debug] HasJobOnCell for {cell}: Pawn cannot reserve cell!");
      return false;
    }

    if (!pawn.CanReach(cell, PathEndMode.Touch, Danger.Deadly, false, false, TraverseMode.ByPawn))
    {
      Log.Message($"[Stratum Debug] HasJobOnCell for {cell}: Pawn cannot reach cell!");
      return false;
    }

    Log.Message($"[Stratum Debug] HasJobOnCell for {cell} succeeded!");
    return true;
  }

  public override Job JobOnCell(Pawn pawn, IntVec3 cell, bool forced = false)
  {
    return JobMaker.MakeJob(DefDatabase<JobDef>.GetNamed("SolarWeb-Stratum-CleanSkylight"), cell);
  }
}
