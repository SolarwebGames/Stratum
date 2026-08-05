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
  public bool enableSkylightLighting = true;
  public bool enableSkylightShadows = true;
  public bool enableDirtGraphics = true;
  public bool enablePollenGraphics = true;
  public bool enableSnowGraphics = true;
  public bool enableRoofDamageScratches = true;
  // Only consulted when a vertical-levels mod reports something stacked above an open cell; with
  // no such mod present this changes nothing. Turn off to restore vanilla sunlight for cells that
  // have no roof of their own regardless of what sits above them.
  public bool enableUpperLevelSkyOcclusion = true;

  public override void ExposeData()
  {
    base.ExposeData();
    Scribe_Values.Look(ref enableRoofFires, "enableRoofFires", true);
    Scribe_Values.Look(ref enableDropPodInterception, "enableDropPodInterception", true);
    Scribe_Values.Look(ref enableSkylightCoating, "enableSkylightCoating", true);
    Scribe_Values.Look(ref skylightDirtAccumulationRate, "skylightDirtAccumulationRate", 1f);
    Scribe_Values.Look(ref enableLightningStrikesTargetRoofs, "enableLightningStrikesTargetRoofs", true);
    Scribe_Values.Look(ref enableRoofLightningExplosions, "enableRoofLightningExplosions", true);
    Scribe_Values.Look(ref enableSkylightLighting, "enableSkylightLighting", true);
    Scribe_Values.Look(ref enableSkylightShadows, "enableSkylightShadows", true);
    Scribe_Values.Look(ref enableDirtGraphics, "enableDirtGraphics", true);
    Scribe_Values.Look(ref enablePollenGraphics, "enablePollenGraphics", true);
    Scribe_Values.Look(ref enableSnowGraphics, "enableSnowGraphics", true);
    Scribe_Values.Look(ref enableRoofDamageScratches, "enableRoofDamageScratches", true);
    Scribe_Values.Look(ref enableUpperLevelSkyOcclusion, "enableUpperLevelSkyOcclusion", true);
  }
}