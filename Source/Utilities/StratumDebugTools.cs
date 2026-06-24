using System.Linq;
using System.Xml.Linq;
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
            var uvEntry = kvp.Value;
            XElement tileElement = new XElement("Tile",
                new XAttribute("col", coord.col),
                new XAttribute("row", coord.row),
                new XElement("UVs",
                    string.Join(" | ", uvEntry.Uvs.Select(u => $"({u.x:F4}, {u.y:F4})"))
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
            var uvEntry = atlasEntry.FlatVariants[i];
            variantsElement.Add(new XElement("Variant",
                new XAttribute("index", i),
                new XElement("UVs", string.Join(" | ", uvEntry.Uvs.Select(u => $"({u.x:F4}, {u.y:F4})")))
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
