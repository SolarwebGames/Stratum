using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

using SolarWeb.Stratum.WorldComponents;

namespace SolarWeb.Stratum.MapComponents;

public class RetractableRoofAnimator : MapComponent
{
  public struct PendingRoof
  {
    public IntVec3 cell;
    public RoofDef def;
    public ThingDef? stuff;
    public Color tint;
    public short hp;
    public int placementTick;
  }

  private List<TransitioningRoof> transitions = [];
  private List<PendingRoof> pendingRoofs = [];

  public RetractableRoofAnimator(Map map) : base(map)
  {
  }

  public void AddTransition(Vector3 start, Vector3 end, int duration, Material mat, Color col, Vector2[] uvs)
  {
    var pool = Find.World.GetComponent<RoofAnimationPool>();
    var t = pool.GetTransitioningRoof(start, end, Find.TickManager.TicksGame, duration, mat, col, uvs);
    transitions.Add(t);
  }

  public void AddPendingRoof(IntVec3 cell, RoofDef def, ThingDef? stuff, Color tint, short hp, int delayTicks)
  {
    pendingRoofs.Add(new PendingRoof
    {
      cell = cell,
      def = def,
      stuff = stuff,
      tint = tint,
      hp = hp,
      placementTick = Find.TickManager.TicksGame + delayTicks
    });
  }

  public override void MapComponentUpdate()
  {
    base.MapComponentUpdate();
    if (transitions.Count == 0 && pendingRoofs.Count == 0) return;

    int currentTick = Find.TickManager.TicksGame;
    float altitude = AltitudeLayer.MoteOverhead.AltitudeFor() - 0.05f;

    for (int i = transitions.Count - 1; i >= 0; i--)
    {
      var t = transitions[i];
      if (t.GetProgress(currentTick) >= 1f)
      {
        var pool = Find.World.GetComponent<RoofAnimationPool>();
        pool.Return(t);
        transitions.RemoveAt(i);
        continue;
      }

      Vector3 pos = t.GetCurrentPosition(currentTick);
      pos.y = altitude;

      Matrix4x4 matrix = Matrix4x4.TRS(pos, Quaternion.identity, Vector3.one);
      
      UnityEngine.Graphics.DrawMesh(t.mesh, matrix, t.material, 0);
    }

    if (pendingRoofs.Count > 0)
    {
      var integrityGrid = map.GetComponent<RoofIntegrityGrid>();
      for (int i = pendingRoofs.Count - 1; i >= 0; i--)
      {
        var p = pendingRoofs[i];
        if (currentTick >= p.placementTick)
        {
          if (map.roofGrid.RoofAt(p.cell) == null)
          {
            map.roofGrid.SetRoof(p.cell, p.def);
            integrityGrid?.InitializeRoof(p.cell, p.def, p.stuff, p.tint, p.hp);
            FleckMaker.ThrowAirPuffUp(p.cell.ToVector3Shifted(), map);
          }
          pendingRoofs.RemoveAt(i);
        }
      }
    }
  }

  public override void MapRemoved()
  {
    base.MapRemoved();
    var pool = Find.World.GetComponent<RoofAnimationPool>();
    foreach (var t in transitions)
    {
      pool.Return(t);
    }
    transitions.Clear();
    pendingRoofs.Clear();
  }
}
