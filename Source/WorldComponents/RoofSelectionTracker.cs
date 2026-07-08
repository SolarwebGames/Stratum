using System.Collections.Generic;
using RimWorld.Planet;
using UnityEngine;
using Verse;

using SolarWeb.Stratum.UI;

namespace SolarWeb.Stratum.WorldComponents;

public class RoofSelectionTracker : WorldComponent
{
  private static RoofSelectionTracker? instance;

  public static RoofSelectionTracker Instance
  {
    get
    {
      if (instance == null || instance.world != Find.World)
      {
        instance = Find.World?.GetComponent<RoofSelectionTracker>();
      }
      return instance!;
    }
  }

  private readonly Dictionary<SelectedRoof, float> roofSelectTimes = new();

  public RoofSelectionTracker(World world) : base(world)
  {
    instance = this;
  }

  public void ClearSelectTimeFor(SelectedRoof sr)
  {
    if (sr == null) return;
    roofSelectTimes.Remove(sr);
  }

  public float GetSelectTimeFor(SelectedRoof sr)
  {
    if (sr == null) return Time.realtimeSinceStartup;

    if (!roofSelectTimes.TryGetValue(sr, out float selectTime))
    {
      selectTime = Time.realtimeSinceStartup;
      roofSelectTimes[sr] = selectTime;
    }
    return selectTime;
  }
}
