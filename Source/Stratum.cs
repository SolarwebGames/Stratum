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
    
    // Roof Fires
    listingStandard.CheckboxLabeled("Stratum_ModSettings_EnableRoofFires".Translate(), ref Settings.enableRoofFires);
    listingStandard.Indent(12);
    listingStandard.Label(Settings.enableRoofFires ? "Stratum_ModSettings_RoofFiresEnabled_Description".Translate() : "Stratum_ModSettings_RoofFiresDisabled_Description".Translate());
    listingStandard.Indent(-12);
    listingStandard.Gap();

    // Drop Pod Interception
    listingStandard.CheckboxLabeled("Stratum_ModSettings_EnableDropPodInterception".Translate(), ref Settings.enableDropPodInterception, "Stratum_ModSettings_EnableDropPodInterception_Desc".Translate());
    listingStandard.Gap();

    // Skylight Coating & Accumulation
    listingStandard.CheckboxLabeled("Stratum_ModSettings_EnableSkylightCoating".Translate(), ref Settings.enableSkylightCoating, "Stratum_ModSettings_EnableSkylightCoating_Desc".Translate());
    if (Settings.enableSkylightCoating)
    {
      listingStandard.Label("Stratum_ModSettings_SkylightDirtAccumulationRate".Translate(Settings.skylightDirtAccumulationRate.ToString("F2")));
      Settings.skylightDirtAccumulationRate = listingStandard.Slider(Settings.skylightDirtAccumulationRate, 0f, 2f);
    }
    listingStandard.Gap();

    // Lightning Targeting Roofs
    listingStandard.CheckboxLabeled("Stratum_ModSettings_EnableLightningStrikesTargetRoofs".Translate(), ref Settings.enableLightningStrikesTargetRoofs, "Stratum_ModSettings_EnableLightningStrikesTargetRoofs_Desc".Translate());
    listingStandard.Gap();

    // Roof Lightning Explosions
    listingStandard.CheckboxLabeled("Stratum_ModSettings_EnableRoofLightningExplosions".Translate(), ref Settings.enableRoofLightningExplosions, "Stratum_ModSettings_EnableRoofLightningExplosions_Desc".Translate());
    listingStandard.Gap();

    // Skylight Shadows and Lighting
    listingStandard.CheckboxLabeled("Stratum_ModSettings_EnableSkylightShadows".Translate(), ref Settings.enableSkylightShadows, "Stratum_ModSettings_EnableSkylightShadows_Desc".Translate());

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

        // 1. Extinguish existing roof fires if disabled
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

        // 2. Clear skylight coatings if disabled
        if (!Settings.enableSkylightCoating)
        {
          var coating = map.GetComponent<MapComponents.SkylightCoating>();
          coating?.ClearAllCoating();
        }

        // 3. Refresh roof & lighting meshes globally
        map.mapDrawer?.WholeMapChanged((ulong)MapMeshFlagDefOf.Roofs);
      }
    }
  }
}