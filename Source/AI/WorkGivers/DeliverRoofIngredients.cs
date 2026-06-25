using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

using SolarWeb.Stratum.Things;

namespace SolarWeb.Stratum.AI.WorkGivers;

public class DeliverRoofIngredients : WorkGiver_Scanner
{
  private const float NearbyStackScanRadius = 5f;
  private const float NearbyNeederScanRadius = 8f;

  public override PathEndMode PathEndMode => PathEndMode.ClosestTouch;

  public override ThingRequest PotentialWorkThingRequest
    => ThingRequest.ForDef(DefOf.ThingDefOf.RoofFrame);

  public override bool ShouldSkip(Pawn pawn, bool forced = false)
    => pawn.Map.listerThings.ThingsOfDef(DefOf.ThingDefOf.RoofFrame).NullOrEmpty();

  public override Job? JobOnCell(Pawn pawn, IntVec3 cell, bool forced = false)
  {
    var frame = cell.GetFirstThing<RoofFrame>(pawn.Map);
    if (frame == null) return null;
    return JobOnThing(pawn, frame, forced);
  }

  public override Job? JobOnThing(Pawn pawn, Thing t, bool forced = false)
  {
    if (t is not RoofFrame frame) return null;
    if (frame.Faction != pawn.Faction) return null;
    if (!pawn.CanReserveAndReach(frame, PathEndMode.ClosestTouch, Danger.Deadly, 1, -1, null, forced)) return null;
    if (frame.HasAllMaterials()) return null;

    var map = pawn.Map;

    foreach (var cost in frame.TotalMaterialCost())
    {
      int needed = frame.GetSpaceRemainingWithEnroute(cost.thingDef, pawn);
      if (needed <= 0) continue;

      if (!map.itemAvailability.ThingsAvailableAnywhere(cost.thingDef, needed, pawn))
      {
        JobFailReason.Is("MissingMaterials".Translate(cost.thingDef.label));
        continue;
      }

      Thing firstStack = GenClosest.ClosestThingReachable(
        pawn.Position, map,
        ThingRequest.ForDef(cost.thingDef),
        PathEndMode.ClosestTouch,
        TraverseParms.For(pawn),
        9999f,
        r => !r.IsForbidden(pawn) && pawn.CanReserve(r));

      if (firstStack == null)
      {
        JobFailReason.Is("NoPath".Translate());
        continue;
      }

      int carryCapacity = pawn.carryTracker.MaxStackSpaceEver(cost.thingDef);
      var stacks = new List<Thing> { firstStack };
      int totalAvailable = Mathf.Min(firstStack.stackCount, carryCapacity);

      if (totalAvailable < carryCapacity)
      {
        foreach (var nearby in GenRadial.RadialDistinctThingsAround(
          firstStack.PositionHeld, firstStack.MapHeld, NearbyStackScanRadius, useCenter: false))
        {
          if (totalAvailable >= carryCapacity) break;
          if (nearby.def == cost.thingDef && GenAI.CanUseItemForWork(pawn, nearby))
          {
            stacks.Add(nearby);
            totalAvailable += nearby.stackCount;
            totalAvailable = Mathf.Min(totalAvailable, carryCapacity);
          }
        }
      }

      int totalNeeded = needed;
      var nearbyNeeders = new List<RoofFrame>();
      foreach (var other in GenRadial.RadialDistinctThingsAround(
        frame.Position, map, NearbyNeederScanRadius, useCenter: false))
      {
        if (totalNeeded >= totalAvailable) break;
        if (!(other is RoofFrame otherFrame) || otherFrame == frame) continue;

        if (!pawn.CanReserveAndReach(otherFrame, PathEndMode.ClosestTouch, Danger.Deadly, 1, -1, null, forced)) continue;

        int otherNeeded = otherFrame.GetSpaceRemainingWithEnroute(cost.thingDef, pawn);
        if (otherNeeded <= 0) continue;
        nearbyNeeders.Add(otherFrame);
        totalNeeded += otherNeeded;
      }

      int toPickUp = 0;
      int keepCount = 0;
      for (int i = 0; i < stacks.Count; i++)
      {
        toPickUp += stacks[i].stackCount;
        toPickUp = Mathf.Min(toPickUp, Mathf.Min(totalAvailable, totalNeeded));
        keepCount = i + 1;
        if (toPickUp >= totalNeeded || toPickUp >= totalAvailable) break;
      }
      stacks.RemoveRange(keepCount, stacks.Count - keepCount);

      var allNeeders = new List<RoofFrame>(nearbyNeeders) { frame };
      RoofFrame primaryNeeder = allNeeders.MinBy(
        f => IntVec3Utility.ManhattanDistanceFlat(firstStack.Position, f.Position));
      allNeeders.Remove(primaryNeeder);

      var job = JobMaker.MakeJob(DefOf.JobDefOf.DeliverRoofIngredients, firstStack, primaryNeeder);
      job.count = toPickUp;

      if (stacks.Count > 1)
      {
        job.targetQueueA = [.. stacks.Skip(1)];
      }

      if (allNeeders.Count > 0)
      {
        job.targetQueueB = [.. allNeeders];
      }

      return job;
    }

    return null;
  }
}
