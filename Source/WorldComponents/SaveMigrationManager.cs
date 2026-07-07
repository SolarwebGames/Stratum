using RimWorld;
using RimWorld.Planet;
using Verse;

using SolarWeb.Stratum.MapComponents;

namespace SolarWeb.Stratum.WorldComponents;

public class SaveMigrationManager(World world) : WorldComponent(world)
{
  private const int CurrentDataVersion = 2;
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
      case 2:
        PerformMigration_v2();
        break;
    }
  }

  private void PerformMigration_v1()
  {
    foreach (var map in Find.Maps)
    {
      ClearVanillaRoofAreas(map);
      map.GetComponent<RoofIntegrityGrid>()?.ExecuteScan(force: true);
      map.mapDrawer?.WholeMapChanged((ulong)MapMeshFlagDefOf.Roofs);
    }
  }

  private void PerformMigration_v2()
  {
    foreach (var map in Find.Maps)
    {
      var integrity = map.GetComponent<RoofIntegrityGrid>();
      if (integrity == null) continue;

      integrity.InitializeNaturalRoofsStuff();

      var numCells = map.cellIndices.NumGridCells;
      var roofGrid = map.roofGrid;
      var hitPoints = integrity.HitPointsArray;

      for (int i = 0; i < numCells; i++)
      {
        var roof = roofGrid.RoofAt(i);
        if (roof != null && Stats.RoofStatCache.IsCustomRoof(roof) && roof.isNatural)
        {
          var cell = map.cellIndices.IndexToCell(i);
          short loadedHP = hitPoints[i];

          if (loadedHP > 0)
          {
            int oldMaxHP = Stats.RoofStatCache.GetMaxHitPoints(roof, null);
            if (oldMaxHP > 0)
            {
              var stuff = integrity.GetStuff(cell);
              int newMaxHP = Stats.RoofStatCache.GetMaxHitPoints(roof, stuff);
              if (newMaxHP != oldMaxHP)
              {
                float ratio = (float)loadedHP / oldMaxHP;
                short newHP = (short)UnityEngine.Mathf.Clamp(UnityEngine.Mathf.RoundToInt(ratio * newMaxHP), 1, newMaxHP);
                hitPoints[i] = newHP;

                if (newHP < newMaxHP)
                {
                  integrity.RoofsNeedingRepair.Add(i);
                }
                else
                {
                  integrity.RoofsNeedingRepair.Remove(i);
                }
              }
            }
          }
        }
      }
    }
  }

  private static void ClearVanillaRoofAreas(Map map)
  {
    var areaManager = map.areaManager;
    if (areaManager == null) return;
    areaManager.BuildRoof?.Clear();
    areaManager.NoRoof?.Clear();
  }
}
