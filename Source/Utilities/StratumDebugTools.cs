using System.Linq;
using System.Xml.Linq;
using UnityEngine;
using LudeonTK;
using RimWorld;
using Verse;
using SolarWeb.Stratum.Graphics;
using SolarWeb.Stratum.Stats;
using SolarWeb.Stratum.Explosions;

namespace SolarWeb.Stratum.Utilities;

public static class StratumDebugTools
{
  [DebugAction("Stratum", "Roof Explosion (Small)", false, false, false, false, false, 0, false, actionType = DebugActionType.ToolMap, allowedGameStates = AllowedGameStates.PlayingOnMap)]
  public static void RoofExplosionSmall()
  {
    GenRoofExplosion.DoExplosion(new ExplosionConfig
    {
      center = Verse.UI.MouseCell(),
      map = Find.CurrentMap,
      radius = 0.9f,
      damType = DamageDefOf.Bomb,
      instigator = null,
      damAmount = 50
    });
  }

  [DebugAction("Stratum", "Roof Explosion (Large)", false, false, false, false, false, 0, false, actionType = DebugActionType.ToolMap, allowedGameStates = AllowedGameStates.PlayingOnMap)]
  public static void RoofExplosionLarge()
  {
    GenRoofExplosion.DoExplosion(new ExplosionConfig
    {
      center = Verse.UI.MouseCell(),
      map = Find.CurrentMap,
      radius = 1.9f,
      damType = DamageDefOf.Bomb,
      instigator = null,
      damAmount = 150
    });
  }

  [DebugAction("Stratum", "Add Roof Snow (10%)", false, false, false, false, false, 0, false, actionType = DebugActionType.ToolMap, allowedGameStates = AllowedGameStates.PlayingOnMap)]
  public static void AddRoofSnow10()
  {
    IntVec3 cell = Verse.UI.MouseCell();
    Map map = Find.CurrentMap;
    if (cell.InBounds(map))
    {
      var coating = map.GetComponent<MapComponents.SkylightCoating>();
      if (coating != null)
      {
        float cur = coating.GetSnowLevel(cell);
        coating.SetSnowLevel(cell, Mathf.Min(1f, cur + 0.10f));
      }
    }
  }

  [DebugAction("Stratum", "Set Roof Snow (100%)", false, false, false, false, false, 0, false, actionType = DebugActionType.ToolMap, allowedGameStates = AllowedGameStates.PlayingOnMap)]
  public static void SetRoofSnow100()
  {
    IntVec3 cell = Verse.UI.MouseCell();
    Map map = Find.CurrentMap;
    if (cell.InBounds(map))
    {
      var coating = map.GetComponent<MapComponents.SkylightCoating>();
      if (coating != null)
      {
        coating.SetSnowLevel(cell, 1f);
      }
    }
  }

  [DebugAction("Stratum", "Clear Roof Snow (0%)", false, false, false, false, false, 0, false, actionType = DebugActionType.ToolMap, allowedGameStates = AllowedGameStates.PlayingOnMap)]
  public static void ClearRoofSnow0()
  {
    IntVec3 cell = Verse.UI.MouseCell();
    Map map = Find.CurrentMap;
    if (cell.InBounds(map))
    {
      var coating = map.GetComponent<MapComponents.SkylightCoating>();
      if (coating != null)
      {
        coating.SetSnowLevel(cell, 0f);
      }
    }
  }

  [DebugAction("Stratum", "Export Roof Graphics Data", false, false, false, false, false, 0, false)]
  public static void ExportRoofGraphicsData()
  {
    XElement root = new XElement("RoofGraphicsData");

    foreach (var def in DefDatabase<RoofDef>.AllDefs)
    {
      var gd = RoofStatCache.GetGraphicData(def);
      if (gd == null) continue;

      XElement roofElement = new XElement("Roof",
          new XAttribute("defName", def.defName),
          new XElement("TexPath", gd.texPath),
          new XElement("IsSeamless", gd.isSeamless)
      );

      if (RoofAtlasManager.uvMap.TryGetValue(gd.texPath, out var atlasEntry))
      {
        XElement atlasElement = new XElement("Atlas",
            new XAttribute("gridWidth", atlasEntry.GridWidth),
            new XAttribute("gridHeight", atlasEntry.GridHeight)
        );

        if (atlasEntry.SeamlessGrid != null)
        {
          XElement gridElement = new XElement("SeamlessGrid");
          foreach (var kvp in atlasEntry.SeamlessGrid.OrderBy(k => k.Key.row).ThenBy(k => k.Key.col))
          {
            var coord = kvp.Key;
            var uvs = kvp.Value;
            XElement tileElement = new XElement("Tile",
                new XAttribute("col", coord.col),
                new XAttribute("row", coord.row),
                new XElement("UVs",
                    string.Join(" | ", uvs.Select(u => $"({u.x:F4}, {u.y:F4})"))
                )
            );
            gridElement.Add(tileElement);
          }
          atlasElement.Add(gridElement);
        }

        if (atlasEntry.FlatVariants.Count > 0)
        {
          XElement variantsElement = new XElement("FlatVariants");
          for (int i = 0; i < atlasEntry.FlatVariants.Count; i++)
          {
            var uvs = atlasEntry.FlatVariants[i];
            variantsElement.Add(new XElement("Variant",
                new XAttribute("index", i),
                new XElement("UVs", string.Join(" | ", uvs.Select(u => $"({u.x:F4}, {u.y:F4})")))
            ));
          }
          atlasElement.Add(variantsElement);
        }
        roofElement.Add(atlasElement);
      }

      root.Add(roofElement);
    }

    string path = System.IO.Path.Combine(GenFilePaths.ConfigFolderPath, "Stratum_RoofGraphicsDebug.xml");
    var doc = new XDocument(root);
    doc.Save(path);
    Messages.Message("Exported roof graphics data to " + path, MessageTypeDefOf.TaskCompletion, false);
  }
}
