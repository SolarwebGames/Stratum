using RimWorld;
using UnityEngine;
using Verse;
using System.Collections.Generic;

using SolarWeb.Stratum.Stats;
using SolarWeb.Stratum.Utilities;

namespace SolarWeb.Stratum.Graphics;

[StaticConstructorOnStartup]
public class CustomRoofBuildingsRenderer : SectionLayer
{
  public CustomRoofBuildingsRenderer(Section section) : base(section)
  {
    relevantChangeTypes = (ulong)MapMeshFlagDefOf.Buildings | (ulong)MapMeshFlagDefOf.Roofs | (ulong)MapMeshFlagDefOf.FogOfWar;
  }

  public override bool Visible => true;

  public override void DrawLayer()
  {
    if (Find.PlaySettings.showRoofOverlay)
    {
      base.DrawLayer();
    }
  }

  public override void Regenerate()
  {
    ClearSubMeshes(MeshParts.All);

    Map map = base.Map;
    if (map == null || map.fogGrid == null || map.thingGrid == null) return;

    CellRect cellRect = new(section.botLeft.x, section.botLeft.z, 17, 17);
    cellRect.ClipInsideMap(map);

    foreach (IntVec3 c in cellRect)
    {
      if (map.fogGrid.IsFogged(c)) continue;

      var things = map.thingGrid.ThingsListAt(c);
      for (int i = 0; i < things.Count; i++)
      {
        Thing thing = things[i];
        if (RoofBuildings.IsRoofBuildingOrBlueprintOrFrame(thing) && RoofBuildings.ShouldRenderRoofBuilding(thing))
        {
          if (thing.Graphic != null && thing is not Frame && thing is not Blueprint)
          {
            Graphic graphic = GetCurrentGraphic(thing);
            graphic?.Print(this, thing, 0f);
          }
        }
      }
    }

    FinalizeMesh(MeshParts.All);
  }

  private static Graphic GetCurrentGraphic(Thing thing)
  {
    var flickable = thing.TryGetComp<CompFlickable>();
    var powerTrader = thing.TryGetComp<CompPowerTrader>();

    bool isOff = (flickable != null && !flickable.SwitchIsOn) || (powerTrader != null && !powerTrader.PowerOn);

    if (isOff && OffTextureExists(thing.def))
    {
      if (flickable != null)
      {
        return flickable.CurrentGraphic;
      }

      if (thing.def.graphicData != null)
      {
        string offTexPath = thing.def.graphicData.texPath + "_Off";
        return GraphicDatabase.Get(
          thing.def.graphicData.graphicClass,
          offTexPath,
          thing.def.graphicData.shaderType.Shader,
          thing.def.graphicData.drawSize,
          thing.DrawColor,
          thing.DrawColorTwo
        );
      }
    }

    return thing.Graphic;
  }

  private static bool OffTextureExists(ThingDef def)
  {
    if (def.graphicData == null) return false;
    string offPath = def.graphicData.texPath + "_Off";
    if (ContentFinder<Texture2D>.Get(offPath, false) != null) return true;
    if (ContentFinder<Texture2D>.Get(offPath + "_north", false) != null) return true;
    if (ContentFinder<Texture2D>.Get(offPath + "_south", false) != null) return true;
    return false;
  }
}
