using Verse;
using SolarWeb.Stratum.Hooks;
using SolarWeb.Stratum.Utilities;

namespace SolarWeb.Stratum.MapComponents;

public class RoofBuildingTracker : MapComponent
{
  private bool lastShowRoofBuildings;
  private bool lastIsPlacingOrDesignating;
  private bool lastShowRoofOverlay;

  public RoofBuildingTracker(Map map) : base(map)
  {
  }

  public override void FinalizeInit()
  {
    base.FinalizeInit();
    var registry = MapHookRegistry.Get(map);
    if (registry != null)
    {
      registry.OnRoofChanged += OnRoofChanged;
    }
  }

  public override void MapRemoved()
  {
    base.MapRemoved();
    var registry = MapHookRegistry.Get(map);
    if (registry != null)
    {
      registry.OnRoofChanged -= OnRoofChanged;
    }
  }

  private void OnRoofChanged(Map m, IntVec3 cell, RoofDef? oldRoof, RoofDef? newRoof)
  {
    if (m != map) return;
    if (newRoof == null)
    {
      RoofBuildings.HandleRoofLoss(map, cell);
    }
  }

  public override void MapComponentUpdate()
  {
    bool showOverlay = Find.PlaySettings.showRoofOverlay;
    bool showBuildings = RoofBuildings.showRoofBuildings;
    bool isPlacing = RoofBuildings.IsPlacingOrDesignatingRoofs(map);

    if (showOverlay != lastShowRoofOverlay || showBuildings != lastShowRoofBuildings || isPlacing != lastIsPlacingOrDesignating)
    {
      lastShowRoofOverlay = showOverlay;
      lastShowRoofBuildings = showBuildings;
      lastIsPlacingOrDesignating = isPlacing;

      RoofBuildings.DirtyAllRoofBuildingCells(map);
    }
  }
}
