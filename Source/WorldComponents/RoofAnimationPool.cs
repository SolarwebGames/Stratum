using System.Collections.Generic;
using RimWorld.Planet;
using UnityEngine;

using SolarWeb.Stratum.MapComponents;

namespace SolarWeb.Stratum.WorldComponents;

public class RoofAnimationPool(World world) : WorldComponent(world)
{
  private readonly Stack<TransitioningRoof> roofPool = new();
  private readonly Stack<Mesh> meshPool = new();

  public TransitioningRoof GetTransitioningRoof(Vector3 start, Vector3 end, int currentTick, int duration, Material mat, Color col, Vector2[] uvs)
  {
    TransitioningRoof t;
    if (roofPool.Count > 0)
    {
      t = roofPool.Pop();
    }
    else
    {
      t = new TransitioningRoof();
    }

    Mesh m;
    if (meshPool.Count > 0)
    {
      m = meshPool.Pop();
    }
    else
    {
      m = new Mesh();
    }

    t.Init(start, end, currentTick, duration, mat, col, uvs, m);
    return t;
  }

  public void Return(TransitioningRoof t)
  {
    if (t.mesh != null)
    {
      t.mesh.Clear();
      meshPool.Push(t.mesh);
      t.mesh = null!;
    }

    t.material = null!;
    roofPool.Push(t);
  }

}
