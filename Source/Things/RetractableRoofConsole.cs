using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

using SolarWeb.Stratum.DefModExtensions;
using SolarWeb.Stratum.MapComponents;
using SolarWeb.Stratum.Stats;

namespace SolarWeb.Stratum.Things;

public class RetractableRoofConsole : Building
{
  public bool isOpeningRequested;
  public bool isTransitioning;
  public bool jobPending;
  public int currentRingIndex;
  public float transitionProgress;

  private List<List<IntVec3>> transitionRings = [];
  private HashSet<IntVec3> canopyCells = [];
  private Vector3 roomCentroid;
  private CompPowerTrader? powerComp;

  public override void SpawnSetup(Map map, bool respawningAfterLoad)
  {
    base.SpawnSetup(map, respawningAfterLoad);
    powerComp = GetComp<CompPowerTrader>();

    if (respawningAfterLoad && isTransitioning)
    {
      RecalculateRings();
    }
  }

  public override void ExposeData()
  {
    base.ExposeData();
    Scribe_Values.Look(ref isOpeningRequested, "isOpeningRequested", false);
    Scribe_Values.Look(ref isTransitioning, "isTransitioning", false);
    Scribe_Values.Look(ref jobPending, "jobPending", false);
    Scribe_Values.Look(ref currentRingIndex, "currentRingIndex", 0);
    Scribe_Values.Look(ref transitionProgress, "transitionProgress", 0f);
  }

  public override IEnumerable<Gizmo> GetGizmos()
  {
    foreach (var g in base.GetGizmos()) yield return g;

    if (powerComp != null && powerComp.PowerOn)
    {
      if (!isTransitioning)
      {
        bool targetState = !isOpeningRequested;
        yield return new Command_Action
        {
          defaultLabel = targetState ? "Stratum_OpenCanopy".Translate() : "Stratum_CloseCanopy".Translate(),
          defaultDesc = targetState ? "Stratum_OpenCanopyDesc".Translate() : "Stratum_CloseCanopyDesc".Translate(),
          icon = targetState ? TexCommand.Install : TexCommand.ForbidOff,
          action = () =>
          {
            isOpeningRequested = targetState;
            jobPending = true;
          }
        };
      }
      else
      {
        yield return new Command_Action
        {
          defaultLabel = "Stratum_CancelTransition".Translate(),
          defaultDesc = "Stratum_CancelTransitionDesc".Translate(),
          icon = TexCommand.ClearPrioritizedWork,
          action = () =>
          {
            isTransitioning = false;
            jobPending = false;
          }
        };
      }
    }
  }

  public float GetActivePowerDraw()
  {
    return Mathf.Max(500f, 50f * canopyCells.Count);
  }

  public override string GetInspectString()
  {
    string str = base.GetInspectString();

    if (canopyCells.Count == 0 && this.IsHashIntervalTick(60))
    {
      RecalculateRings();
    }

    string statusKey = "Stratum_CanopyIdle";
    if (isTransitioning)
    {
      statusKey = isOpeningRequested ? "Stratum_CanopyOpening" : "Stratum_CanopyClosing";
    }
    else if (canopyCells.Count > 0)
    {
      int openCount = 0;
      foreach (var c in canopyCells)
      {
        if (Map.roofGrid.RoofAt(c) == null) openCount++;
      }
      if (openCount == canopyCells.Count) statusKey = "Stratum_CanopyRetracted";
      else if (openCount == 0) statusKey = "Stratum_CanopyExtended";
      else statusKey = "Stratum_CanopyPartiallyRetracted";
    }

    if (!str.NullOrEmpty()) str += "\n";
    str += "Stratum_CanopyStatus".Translate(statusKey.Translate()) + "\n";
    str += "Stratum_ConnectedTiles".Translate(canopyCells.Count) + "\n";
    str += "Stratum_TransitionPower".Translate(GetActivePowerDraw());

    return str;
  }

  public override void DrawExtraSelectionOverlays()
  {
    base.DrawExtraSelectionOverlays();

    if (canopyCells.Count > 0)
    {
      GenDraw.DrawFieldEdges(canopyCells.ToList(), Color.cyan, null);
    }
  }

  public void InitiateTransition()
  {
    RecalculateRings();
    isTransitioning = true;
    jobPending = false;
    transitionProgress = 0f;

    if (isOpeningRequested)
    {
      currentRingIndex = 0;
      while (currentRingIndex < transitionRings.Count && RingIsAlreadyOpen(transitionRings[currentRingIndex]))
        currentRingIndex++;
    }
    else
    {
      currentRingIndex = transitionRings.Count - 1;
      while (currentRingIndex >= 0 && RingIsAlreadyClosed(transitionRings[currentRingIndex]))
        currentRingIndex--;
    }
  }

  private bool IsRetractableRoof(RoofDef roof)
  {
    return roof != null && roof.GetModExtension<BuildableRoofExtension>()?.isRetractable == true;
  }

  private bool RingIsAlreadyOpen(List<IntVec3> ring)
  {
    foreach (var cell in ring)
    {
      var roof = Map.roofGrid.RoofAt(cell);
      if (IsRetractableRoof(roof))
        return false;
    }
    return true;
  }

  private bool RingIsAlreadyClosed(List<IntVec3> ring)
  {
    foreach (var cell in ring)
    {
      var roof = Map.roofGrid.RoofAt(cell);
      if (!IsRetractableRoof(roof))
        return false;
    }
    return true;
  }

  private void RecalculateRings()
  {
    var tracker = Map.GetComponent<RetractableRoofTracker>();
    HashSet<IntVec3> validCells;

    var room = this.GetRoom();
    if (room == null)
    {
      transitionRings.Clear();
      return;
    }

    var roomValidCells = new HashSet<IntVec3>();
    foreach (var cell in room.Cells)
    {
      int idx = Map.cellIndices.CellToIndex(cell);
      var r = Map.roofGrid.RoofAt(cell);

      bool isValid = false;
      if (IsRetractableRoof(r)) isValid = true;
      else if (tracker != null && tracker.IsRetracted(idx)) isValid = true;

      if (isValid)
      {
        roomValidCells.Add(cell);
      }
    }

    if (roomValidCells.Count == 0)
    {
      transitionRings.Clear();
      return;
    }

    if (!room.TouchesMapEdge)
    {
      validCells = roomValidCells;
    }
    else
    {
      var unvisited = new HashSet<IntVec3>(roomValidCells);
      var components = new List<HashSet<IntVec3>>();

      while (unvisited.Count > 0)
      {
        var comp = new HashSet<IntVec3>();
        var queue = new Queue<IntVec3>();

        var start = unvisited.First();
        queue.Enqueue(start);
        unvisited.Remove(start);
        comp.Add(start);

        while (queue.Count > 0)
        {
          var curr = queue.Dequeue();
          for (int i = 0; i < 8; i++)
          {
            IntVec3 n = curr + GenAdj.AdjacentCells[i];
            if (unvisited.Contains(n))
            {
              unvisited.Remove(n);
              comp.Add(n);
              queue.Enqueue(n);
            }
          }
        }
        components.Add(comp);
      }

      HashSet<IntVec3> closestComp = components[0];
      float minDist = float.MaxValue;

      Vector3 consolePos = Position.ToVector3Shifted();
      foreach (var comp in components)
      {
        float dist = float.MaxValue;
        foreach (var c in comp)
        {
          float d = Vector3.Distance(c.ToVector3Shifted(), consolePos);
          if (d < dist) dist = d;
        }
        if (dist < minDist)
        {
          minDist = dist;
          closestComp = comp;
        }
      }

      validCells = closestComp;
    }

    float sumX = 0;
    float sumZ = 0;
    foreach (var c in validCells)
    {
      sumX += c.x;
      sumZ += c.z;
    }
    roomCentroid = new Vector3(sumX / validCells.Count, 0f, sumZ / validCells.Count);
    Vector3 centroid = roomCentroid;

    var dict = new SortedDictionary<float, List<IntVec3>>();
    foreach (var c in validCells)
    {
      float dist = Vector3.Distance(c.ToVector3Shifted(), centroid);
      float roundedDist = Mathf.Round(dist * 2f) / 2f;
      if (!dict.TryGetValue(roundedDist, out var list))
      {
        list = [];
        dict[roundedDist] = list;
      }
      list.Add(c);
    }

    transitionRings = dict.Values.ToList();
    canopyCells = validCells;
  }

  protected override void Tick()
  {
    base.Tick();

    if (!isTransitioning)
    {
      if (Find.Selector.IsSelected(this) && this.IsHashIntervalTick(60))
      {
        RecalculateRings();
      }

      if (powerComp != null)
      {
        powerComp.PowerOutput = -200f;
      }
      return;
    }

    if (powerComp != null && !powerComp.PowerOn)
    {
      var flick = GetComp<CompFlickable>();
      if (flick != null && !flick.SwitchIsOn) return;
      
      var breakdown = GetComp<CompBreakdownable>();
      if (breakdown != null && breakdown.BrokenDown) return;
    }

    float desiredPower = GetActivePowerDraw();

    if (powerComp != null)
    {
      powerComp.PowerOutput = -desiredPower;
    }

    if (transitionRings.Count == 0 ||
       (isOpeningRequested && currentRingIndex >= transitionRings.Count) ||
       (!isOpeningRequested && currentRingIndex < 0))
    {
      isTransitioning = false;
      return;
    }

    int animationDuration = 1; // Default minimum to prevent div by 0
    var tracker = Map.GetComponent<RetractableRoofTracker>();
    var currentRing = transitionRings[currentRingIndex];
    foreach (var cell in currentRing)
    {
      var rDef = Map.roofGrid.RoofAt(cell);

      if (rDef == null && tracker != null)
      {
        int index = Map.cellIndices.CellToIndex(cell);
        tracker.PeekOpenRoof(index, out rDef, out _, out _, out _);
      }

      if (rDef != null)
      {
        var ext = rDef.GetModExtension<BuildableRoofExtension>();
        if (ext != null && ext.isRetractable)
        {
          if (ext.transitionTicksPerRing > animationDuration)
          {
            animationDuration = ext.transitionTicksPerRing;
          }
        }
      }
    }

    int ticksToNextRing = Mathf.Max(1, animationDuration / 6);

    float speedMultiplier = 1f;
    transitionProgress += speedMultiplier;

    if (transitionProgress >= ticksToNextRing)
    {
      transitionProgress = 0f;

      var ring = transitionRings[currentRingIndex];
      var integrityGrid = Map.GetComponent<RoofIntegrityGrid>();

      foreach (var cell in ring)
      {
        int index = Map.cellIndices.CellToIndex(cell);

        if (isOpeningRequested)
        {
          var roof = Map.roofGrid.RoofAt(cell);
          if (IsRetractableRoof(roof))
          {
            bool bordersEmptyAir = false;
            for (int i = 0; i < 8; i++)
            {
              IntVec3 n = cell + GenAdj.AdjacentCells[i];
              if (!n.InBounds(Map)) continue;

              if (!canopyCells.Contains(n))
              {
                bool hasRoof = Map.roofGrid.RoofAt(n) != null;
                bool hasEdifice = n.GetEdifice(Map) != null;
                if (!hasRoof && !hasEdifice)
                {
                  bordersEmptyAir = true;
                  break;
                }
              }
            }

            if (bordersEmptyAir)
            {
              continue;
            }

            var stuff = integrityGrid?.GetStuff(cell);
            var tint = integrityGrid?.GetGlassTint(cell);
            var hp = integrityGrid?.GetHitPoints(cell) ?? (short)180;

            tracker?.SaveOpenRoof(index, roof, stuff, tint, hp);

            var gd = RoofStatCache.GetGraphicData(roof);
            if (gd != null)
            {
              bool gotUv = gd.isSeamless
                ? Graphics.RoofAtlasManager.TryGetSeamlessUv(gd.texPath, cell.x, cell.z, out var uvs, out var mat)
                : Graphics.RoofAtlasManager.TryGetUv(gd.texPath, cell.GetHashCode(), out uvs, out mat);

              if (gotUv)
              {
                var animator = Map.GetComponent<RetractableRoofAnimator>();
                if (animator != null)
                {
                  Vector3 start = cell.ToVector3Shifted();
                  Vector3 rawDir = (start - roomCentroid).normalized;
                  Vector3 dir = Mathf.Abs(rawDir.x) > Mathf.Abs(rawDir.z) 
                    ? new Vector3(Mathf.Sign(rawDir.x), 0, 0) 
                    : new Vector3(0, 0, Mathf.Sign(rawDir.z));
                  Vector3 end = start + dir * 1f;

                  Color color = RoofStatCache.GetColor(roof, stuff);
                  if (RoofStatCache.IsSkylight(roof))
                  {
                    color.a = 1f - RoofStatCache.GetTransparency(roof);
                  }
                  animator.AddTransition(start, end, animationDuration, mat!, color, uvs!);
                }
              }
            }

            Map.roofGrid.SetRoof(cell, null);
            FleckMaker.ThrowAirPuffUp(cell.ToVector3Shifted(), Map);
          }
        }
        else
        {
          if (tracker != null && tracker.PopOpenRoof(index, out var rDef, out var stuff, out var tint, out var hp))
          {
            if (Map.roofGrid.RoofAt(cell) == null)
            {
              var gd = RoofStatCache.GetGraphicData(rDef);
              var animator = Map.GetComponent<RetractableRoofAnimator>();
              if (gd != null)
              {
                bool gotUv = gd.isSeamless
                  ? Graphics.RoofAtlasManager.TryGetSeamlessUv(gd.texPath, cell.x, cell.z, out var uvs, out var mat)
                  : Graphics.RoofAtlasManager.TryGetUv(gd.texPath, cell.GetHashCode(), out uvs, out mat);

                if (gotUv)
                {
                  if (animator != null)
                  {
                    Vector3 end = cell.ToVector3Shifted();
                    Vector3 rawDir = (end - roomCentroid).normalized;
                    Vector3 dir = Mathf.Abs(rawDir.x) > Mathf.Abs(rawDir.z) 
                      ? new Vector3(Mathf.Sign(rawDir.x), 0, 0) 
                      : new Vector3(0, 0, Mathf.Sign(rawDir.z));
                    Vector3 start = end + dir * 1f;

                    Color color = RoofStatCache.GetColor(rDef, stuff);
                    if (RoofStatCache.IsSkylight(rDef))
                    {
                      color.a = 1f - RoofStatCache.GetTransparency(rDef);
                    }
                    animator.AddTransition(start, end, animationDuration, mat!, color, uvs!);
                  }
                }
              }

              if (animator != null)
              {
                animator.AddPendingRoof(cell, rDef, stuff, tint ?? Color.white, hp, animationDuration);
              }
              else
              {
                Map.roofGrid.SetRoof(cell, rDef);
                integrityGrid?.InitializeRoof(cell, rDef, stuff, tint, hp);
                FleckMaker.ThrowAirPuffUp(cell.ToVector3Shifted(), Map);
              }
            }
          }
        }
      }

      DefDatabase<SoundDef>.GetNamed("DropPod_Open", false)?.PlayOneShot(new TargetInfo(Position, Map));

      if (isOpeningRequested)
        currentRingIndex++;
      else
        currentRingIndex--;
    }
  }
}
