using RimWorld;
using RimWorld.Planet;
using Verse;

using SolarWeb.Stratum.MapComponents;

namespace SolarWeb.Stratum.WorldComponents;

public class SaveMigrationManager(World world) : WorldComponent(world)
{
  private const int CurrentDataVersion = 1;
  private int lastMigratedDataVersion = 0;
  private string lastPlayedModVersion = "0.0.0";

  private static string? cachedModVersion;
  public static string ModVersion => cachedModVersion ??= ModLister.GetModWithIdentifier(Stratum.Identifier)?.ModVersion ?? "Unknown";

  public override void ExposeData()
  {
    base.ExposeData();
    
    Scribe_Values.Look(ref lastMigratedDataVersion, "lastMigratedVersion", 0);
    Scribe_Values.Look(ref lastPlayedModVersion, "lastPlayedModVersion", "0.0.0");
  }

  public override void FinalizeInit(bool fromLoad)
  {
    base.FinalizeInit(fromLoad);
    
    string currentModVersion = ModVersion;
    if (lastMigratedDataVersion < CurrentDataVersion)
    {
      StratumLog.Message($"Starting save migration (Mod Version: {lastPlayedModVersion} -> {currentModVersion})...");
      while (lastMigratedDataVersion < CurrentDataVersion)
      {
        lastMigratedDataVersion++;
        RunMigration(lastMigratedDataVersion);
      }
      StratumLog.Message($"Save migration to Data Version {CurrentDataVersion} complete.");
    }
    
    lastPlayedModVersion = currentModVersion;
  }

  private void RunMigration(int dataVersion)
  {
    StratumLog.Message($"Running migration step {dataVersion}...");
    switch (dataVersion)
    {
      case 1:
        PerformMigration_v1();
        break;
    }
  }

  private void PerformMigration_v1()
  {
    foreach (var map in Find.Maps)
    {
      ClearVanillaRoofAreas(map);
      map.GetComponent<RoofIntegrityGrid>()?.ExecuteScan(force: true);
      map.mapDrawer.WholeMapChanged((ulong)MapMeshFlagDefOf.Roofs);
    }
  }

  private static void ClearVanillaRoofAreas(Map map)
  {
    var areaManager = map.areaManager;
    areaManager.BuildRoof?.Clear();
    areaManager.NoRoof?.Clear();
  }
}
