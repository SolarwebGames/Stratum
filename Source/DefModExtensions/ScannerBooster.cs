using RimWorld;
using Verse;

namespace SolarWeb.Stratum.DefModExtensions;

public class ScannerBooster : DefModExtension
{
  public float scanSpeedMultiplier = 1.5f;
  public float range = 20f;

  public static float GetBoosterMultiplier(Map map, IntVec3 position)
  {
    if (map == null) return 1f;

    float maxMultiplier = 1f;
    var buildings = map.listerBuildings.allBuildingsColonist;
    for (int i = 0; i < buildings.Count; i++)
    {
      var b = buildings[i];
      if (b.Spawned)
      {
        var ext = b.def.GetModExtension<ScannerBooster>();
        if (ext != null)
        {
          var power = b.TryGetComp<CompPowerTrader>();
          if (power != null && power.PowerOn)
          {
            if ((position - b.Position).LengthHorizontal <= ext.range)
            {
              if (ext.scanSpeedMultiplier > maxMultiplier)
              {
                maxMultiplier = ext.scanSpeedMultiplier;
              }
            }
          }
        }
      }
    }
    return maxMultiplier;
  }

}
