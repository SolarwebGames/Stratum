using UnityEngine;
using Verse;

namespace SolarWeb.Stratum;

public class Stratum : Mod
{
  public const string Identifier = "SolarWeb.Stratum";

  public static StratumSettings Settings { get; private set; } = default!;

  public Stratum(ModContentPack content) : base(content)
  {
    Settings = GetSettings<StratumSettings>();
  }

  public override string SettingsCategory() => "Stratum";

  public override void DoSettingsWindowContents(Rect inRect)
  {
    Listing_Standard listingStandard = new();
    listingStandard.Begin(inRect);
    listingStandard.CheckboxLabeled("Stratum_ModSettings_EnableRoofFires".Translate(), ref Settings.enableRoofFires);
    listingStandard.Indent(12);
    listingStandard.Label(Settings.enableRoofFires ? "Stratum_ModSettings_RoofFiresEnabled_Description".Translate() : "Stratum_ModSettings_RoofFiresDisabled_Description".Translate());
    listingStandard.Indent(-12);
    listingStandard.End();
    base.DoSettingsWindowContents(inRect);
  }
}