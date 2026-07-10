using Verse;
using SolarWeb.Stratum.Patches;

namespace SolarWeb.Stratum.MapComponents;

public class LightingOverlayCache : MapComponent
{
  internal readonly SectionLayer_LightingOverlay_Patch.CellGlowData[] cachedGlowData;
  internal readonly int[] lastCellUpdateFrame;

  public LightingOverlayCache(Map map) : base(map)
  {
    int numCells = map.cellIndices.NumGridCells;
    cachedGlowData = new SectionLayer_LightingOverlay_Patch.CellGlowData[numCells];
    lastCellUpdateFrame = new int[numCells];
    for (int i = 0; i < numCells; i++)
    {
      lastCellUpdateFrame[i] = -1;
    }
  }
}
