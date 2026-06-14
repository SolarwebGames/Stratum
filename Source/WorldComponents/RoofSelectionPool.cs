using System.Collections.Generic;
using RimWorld.Planet;
using Verse;

namespace SolarWeb.Stratum.WorldComponents;

public class RoofSelectionPool(World world) : WorldComponent(world)
{
  private readonly Stack<UI.SelectedRoof> pool = new();

  public UI.SelectedRoof Get(Map map, IntVec3 cell, RoofDef def)
  {
    if (pool.Count > 0)
    {
      var obj = pool.Pop();
      obj.Initialize(map, cell, def);
      return obj;
    }
    return new UI.SelectedRoof(map, cell, def);
  }

  public void Return(UI.SelectedRoof obj)
  {
    pool.Push(obj);
  }
}
