using System.Collections.Generic;
using RimWorld;
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

    Text.Font = GameFont.Medium;
    listingStandard.Label("Stratum_ModSettings_Category_Gameplay".Translate());
    Text.Font = GameFont.Small;
    listingStandard.GapLine();
    listingStandard.Gap(6f);

    listingStandard.CheckboxLabeled("Stratum_ModSettings_EnableRoofFires".Translate(), ref Settings.enableRoofFires, Settings.enableRoofFires ? "Stratum_ModSettings_RoofFiresEnabled_Description".Translate() : "Stratum_ModSettings_RoofFiresDisabled_Description".Translate());
    listingStandard.Gap();

    listingStandard.CheckboxLabeled("Stratum_ModSettings_EnableDropPodInterception".Translate(), ref Settings.enableDropPodInterception, Settings.enableDropPodInterception ? "Stratum_ModSettings_DropPodInterceptionEnabled_Desc".Translate() : "Stratum_ModSettings_DropPodInterceptionDisabled_Desc".Translate());
    listingStandard.Gap();

    listingStandard.CheckboxLabeled("Stratum_ModSettings_EnableLightningStrikesTargetRoofs".Translate(), ref Settings.enableLightningStrikesTargetRoofs, Settings.enableLightningStrikesTargetRoofs ? "Stratum_ModSettings_LightningStrikesTargetRoofsEnabled_Desc".Translate() : "Stratum_ModSettings_LightningStrikesTargetRoofsDisabled_Desc".Translate());
    listingStandard.Gap();

    listingStandard.CheckboxLabeled("Stratum_ModSettings_EnableRoofLightningExplosions".Translate(), ref Settings.enableRoofLightningExplosions, Settings.enableRoofLightningExplosions ? "Stratum_ModSettings_RoofLightningExplosionsEnabled_Desc".Translate() : "Stratum_ModSettings_RoofLightningExplosionsDisabled_Desc".Translate());
    listingStandard.Gap(18f);

    Text.Font = GameFont.Medium;
    listingStandard.Label("Stratum_ModSettings_Category_Skylights".Translate());
    Text.Font = GameFont.Small;
    listingStandard.GapLine();
    listingStandard.Gap(6f);

    listingStandard.CheckboxLabeled("Stratum_ModSettings_EnableSkylightCoating".Translate(), ref Settings.enableSkylightCoating, Settings.enableSkylightCoating ? "Stratum_ModSettings_SkylightCoatingEnabled_Desc".Translate() : "Stratum_ModSettings_SkylightCoatingDisabled_Desc".Translate());
    if (Settings.enableSkylightCoating)
    {
      listingStandard.Label("Stratum_ModSettings_SkylightDirtAccumulationRate".Translate(Settings.skylightDirtAccumulationRate.ToString("F2")));
      Settings.skylightDirtAccumulationRate = listingStandard.Slider(Settings.skylightDirtAccumulationRate, 0f, 2f);
    }
    listingStandard.Gap(18f);

    Text.Font = GameFont.Medium;
    listingStandard.Label("Stratum_ModSettings_Category_Graphics".Translate());
    Text.Font = GameFont.Small;
    listingStandard.GapLine();
    listingStandard.Gap(6f);

    listingStandard.CheckboxLabeled("Stratum_ModSettings_EnableSkylightLighting".Translate(), ref Settings.enableSkylightLighting, Settings.enableSkylightLighting ? "Stratum_ModSettings_SkylightLightingEnabled_Desc".Translate() : "Stratum_ModSettings_SkylightLightingDisabled_Desc".Translate());
    listingStandard.Gap();

    listingStandard.CheckboxLabeled("Stratum_ModSettings_EnableSkylightShadows".Translate(), ref Settings.enableSkylightShadows, Settings.enableSkylightShadows ? "Stratum_ModSettings_SkylightShadowsEnabled_Desc".Translate() : "Stratum_ModSettings_SkylightShadowsDisabled_Desc".Translate());
    listingStandard.Gap();

    listingStandard.CheckboxLabeled("Stratum_ModSettings_EnableDirtGraphics".Translate(), ref Settings.enableDirtGraphics, Settings.enableDirtGraphics ? "Stratum_ModSettings_DirtGraphicsEnabled_Desc".Translate() : "Stratum_ModSettings_DirtGraphicsDisabled_Desc".Translate());
    listingStandard.Gap();

    listingStandard.CheckboxLabeled("Stratum_ModSettings_EnablePollenGraphics".Translate(), ref Settings.enablePollenGraphics, Settings.enablePollenGraphics ? "Stratum_ModSettings_PollenGraphicsEnabled_Desc".Translate() : "Stratum_ModSettings_PollenGraphicsDisabled_Desc".Translate());
    listingStandard.Gap();

    listingStandard.CheckboxLabeled("Stratum_ModSettings_EnableSnowGraphics".Translate(), ref Settings.enableSnowGraphics, Settings.enableSnowGraphics ? "Stratum_ModSettings_SnowGraphicsEnabled_Desc".Translate() : "Stratum_ModSettings_SnowGraphicsDisabled_Desc".Translate());
    listingStandard.Gap();

    listingStandard.CheckboxLabeled("Stratum_ModSettings_EnableRoofDamageScratches".Translate(), ref Settings.enableRoofDamageScratches, Settings.enableRoofDamageScratches ? "Stratum_ModSettings_RoofDamageScratchesEnabled_Desc".Translate() : "Stratum_ModSettings_RoofDamageScratchesDisabled_Desc".Translate());

    listingStandard.End();
    base.DoSettingsWindowContents(inRect);
  }

  public override void WriteSettings()
  {
    base.WriteSettings();
    if (Current.ProgramState == ProgramState.Playing)
    {
      var maps = Find.Maps;
      if (maps == null) return;

      for (int i = 0; i < maps.Count; i++)
      {
        var map = maps[i];
        if (map == null) continue;

        if (!Settings.enableRoofFires && map.listerThings != null)
        {
          List<Thing> toExtinguish = [];
          foreach (var thing in map.listerThings.AllThings)
          {
            if (thing is Things.RoofFire)
            {
              toExtinguish.Add(thing);
            }
          }
          for (int j = 0; j < toExtinguish.Count; j++)
          {
            toExtinguish[j].Destroy();
          }
        }

        if (!Settings.enableSkylightCoating)
        {
          var coating = map.GetComponent<MapComponents.SkylightCoating>();
          coating?.ClearAllCoating();
        }

        map.mapDrawer?.WholeMapChanged((ulong)MapMeshFlagDefOf.Roofs);
      }
    }
  }
}