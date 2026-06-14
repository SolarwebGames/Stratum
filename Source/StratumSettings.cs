using Verse;

namespace SolarWeb.Stratum;

public class StratumSettings : ModSettings
{
  public bool enableRoofFires = true;

  public override void ExposeData()
  {
    base.ExposeData();
    Scribe_Values.Look(ref enableRoofFires, "enableRoofFires", true);
  }
}