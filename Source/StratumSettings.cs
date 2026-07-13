using Verse;

namespace SolarWeb.Stratum;

public class StratumSettings : ModSettings
{
  public bool enableRoofFires = true;
  public bool enableDropPodInterception = true;
  public bool enableSkylightCoating = true;
  public float skylightDirtAccumulationRate = 1f;
  public bool enableLightningStrikesTargetRoofs = true;
  public bool enableRoofLightningExplosions = true;
  public bool enableSkylightShadows = true;

  public override void ExposeData()
  {
    base.ExposeData();
    Scribe_Values.Look(ref enableRoofFires, "enableRoofFires", true);
    Scribe_Values.Look(ref enableDropPodInterception, "enableDropPodInterception", true);
    Scribe_Values.Look(ref enableSkylightCoating, "enableSkylightCoating", true);
    Scribe_Values.Look(ref skylightDirtAccumulationRate, "skylightDirtAccumulationRate", 1f);
    Scribe_Values.Look(ref enableLightningStrikesTargetRoofs, "enableLightningStrikesTargetRoofs", true);
    Scribe_Values.Look(ref enableRoofLightningExplosions, "enableRoofLightningExplosions", true);
    Scribe_Values.Look(ref enableSkylightShadows, "enableSkylightShadows", true);
  }
}