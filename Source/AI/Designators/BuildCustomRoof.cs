using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

using SolarWeb.Stratum.DefModExtensions;
using SolarWeb.Stratum.Graphics;
using SolarWeb.Stratum.MapComponents;
using SolarWeb.Stratum.Stats;
using SolarWeb.Stratum.Things;
using SolarWeb.Stratum.UI;
using SolarWeb.Stratum.Utilities;

namespace SolarWeb.Stratum.AI.Designators;

public class BuildCustomRoof : Designator_Build
{
  private readonly RoofDef roofDef;
  private readonly BuildableRoofExtension ext;
  private ThingDef? customStuffDef;
  private bool customWriteStuff;
  private Color? selectedTint;

  public Color? SelectedTint => selectedTint;
  public override DrawStyleCategoryDef DrawStyleCategory => DrawStyleCategoryDefOf.Areas;
  protected override DesignationDef Designation => null!;

  public override ThingDef? StuffDef
  {
    get
    {
      var raw = StuffDefRaw;
      if (raw != null) return raw;
      if (customStuffDef != null) return customStuffDef;
      customStuffDef = RoofStuffUtility.GetCheapestAvailableStuff(PlacingDef, ext, Map);
      return customStuffDef;
    }
  }

  public override string Label
  {
    get
    {
      if (PlacingDef is ThingDef thingDef && (customWriteStuff || StuffDefRaw != null))
      {
        var stuff = StuffDef;
        if (stuff != null)
          return GenLabel.ThingLabel(thingDef, stuff).CapitalizeFirst();
      }
      return base.Label;
    }
  }

  public override Color IconDrawColor
  {
    get
    {
      if (RoofStatCache.IsSkylight(roofDef))
      {
        return selectedTint ?? RoofStatCache.GetGlassTint(roofDef);
      }
      return base.IconDrawColor;
    }
  }

  public BuildCustomRoof(RoofDef roofDef, BuildableRoofExtension ext) : base(ext.buildableDef ?? ThingDefOf.Wall)
  {
    this.roofDef = roofDef;
    this.ext = ext;

    var bDef = ext.buildableDef;
    var gd = RoofStatCache.GetGraphicData(roofDef);

    if (bDef != null)
    {
      Order = bDef.uiOrder;
      defaultLabel = bDef.label;
      defaultDesc = bDef.description;
      icon = bDef.uiIcon;

      if (gd != null)
      {
        defaultIconColor = gd.color;
      }
      else if (bDef is ThingDef tDef && tDef.graphicData != null)
      {
        defaultIconColor = tDef.graphicData.color;
      }
    }
    else
    {
      defaultLabel = roofDef.label;
      defaultDesc = roofDef.description ?? "Stratum_BuildRoofDesc".Translate(roofDef.label);
    }

    UpdateIcon();

    soundDragSustain = SoundDefOf.Designate_DragStandard;
    soundDragChanged = SoundDefOf.Designate_DragStandard_Changed;
    soundSucceeded = SoundDefOf.Designate_ZoneAdd_Roof;
    useMouseIcon = true;
  }

  private void UpdateIcon()
  {
    RoofIconUtility.TryExtractIcon(roofDef, ext, ref icon, ref iconTexCoords);
  }

  public override void DrawIcon(Rect rect, Material? buttonMat, GizmoRenderParms parms)
  {
    RoofIconUtility.DrawDesignatorIcon(rect, roofDef, StuffDef, selectedTint, defaultIconColor, icon, iconTexCoords, buttonMat, parms);
  }

  public override bool Visible
  {
    get
    {
      if (DebugSettings.godMode) return true;
      var bDef = ext.buildableDef;
      if (bDef != null)
      {
        if (bDef.researchPrerequisites != null)
          foreach (var rp in bDef.researchPrerequisites)
            if (!rp.IsFinished) return false;

        if (bDef is ThingDef { MadeFromStuff: true } thingDef)
        {
          bool anyStuff = false;
          foreach (var stuff in GenStuff.AllowedStuffsFor(thingDef))
          {
            if (ext.allowedStuff != null && !ext.allowedStuff.Contains(stuff)) continue;

            if (RoofStuffUtility.GetAccessibleStuffCount(stuff, Map) > 0)
            {
              anyStuff = true;
              break;
            }
          }
          if (!anyStuff) return false;
        }
      }
      return true;
    }
  }

  public override IEnumerable<FloatMenuOption> RightClickFloatMenuOptions => null!;

  public override void DoExtraGuiControls(float leftX, float bottomY)
  {
    if (RoofStatCache.IsSkylight(roofDef))
    {
      Rect rect = new Rect(leftX, bottomY - 30f, 200f, 30f);
      if (Widgets.ButtonText(rect, "Set Glass Tint"))
      {
        Color current = selectedTint ?? RoofStatCache.GetGlassTint(roofDef);
        Color defaultColor = RoofStatCache.GetGlassTint(roofDef);

        Find.WindowStack.Add(new SkylightTintPicker(current, defaultColor, color =>
        {
          selectedTint = color;
          UpdateIcon();
        }));
      }
    }
  }

  public override AcceptanceReport CanDesignateCell(IntVec3 c)
  {
    if (!c.InBounds(Map))
      return false;
    if (c.Fogged(Map))
      return false;

    var currentRoof = c.GetRoof(Map);
    if (currentRoof == roofDef)
      return AcceptanceReport.WasRejected;

    if (currentRoof != null && currentRoof.isThickRoof && currentRoof.isNatural)
      return "MessageRoofIncapableOfRemoving".Translate(currentRoof.label);

    foreach (Thing thing in Map.thingGrid.ThingsAt(c))
    {
      if (thing.def.category == ThingCategory.Plant && thing.def.plant.interferesWithRoof)
      {
        if (thing.TryGetComp<CompPlantPreventCutting>()?.PreventCutting == true)
        {
          return "MessageRoofIncompatibleWithPlant".Translate(thing);
        }
      }
    }

    return AcceptanceReport.WasAccepted;
  }

  public override void ProcessInput(Event ev)
  {
    if (!CheckCanInteract()) return;

    if (PlacingDef is ThingDef { MadeFromStuff: true } thingDef)
    {
      RoofStuffUtility.GenerateStuffSelectionMenu(thingDef, ext, Map, DebugSettings.godMode, (selected) =>
      {
        Find.DesignatorManager.Select(this);
        SetStuffDef(selected);
        customWriteStuff = true;
        UpdateIcon();
      });
    }
    else
    {
      base.ProcessInput(ev);
    }
  }

  public override void DesignateSingleCell(IntVec3 c)
  {
    if (DebugSettings.godMode)
    {
      Map.roofGrid.SetRoof(c, roofDef);
      Map.GetComponent<RoofIntegrityGrid>()?.InitializeRoof(c, roofDef, StuffDef, selectedTint);
      return;
    }

    var tracker = Map.GetComponent<RoofConstructionTracker>();
    tracker.RemoveRecord(c);
    Map.areaManager.NoRoof[c] = false;

    float workToBuild = 1000f;
    var bDef = ext.buildableDef;
    if (bDef != null)
    {
      workToBuild = bDef.statBases.GetStatValueFromList(StatDefOf.WorkToBuild, 1000f);
    }

    tracker.AddRecord(c, roofDef, workToBuild, StuffDef, selectedTint);
    var frame = (RoofFrame)ThingMaker.MakeThing(DefOf.ThingDefOf.RoofFrame);
    frame.targetRoofDef = roofDef;
    frame.targetRoofStuff = StuffDef;
    frame.glassTint = selectedTint;
    frame.SetFaction(Faction.OfPlayer);
    GenSpawn.Spawn(frame, c, Map);
  }

  public override void SelectedUpdate()
  {
    GenUI.RenderMouseoverBracket();
  }

  public override void DrawPanelReadout(ref float curY, float width)
  {
    if (PlacingDef == null) return;

    if (PlacingDef is ThingDef thingDef)
      Widgets.InfoCardButton(width - 24f - 2f, 6f, thingDef, StuffDef);
    else
      Widgets.InfoCardButton(width - 24f - 2f, 6f, PlacingDef);

    Text.Font = GameFont.Small;

    var costList = PlacingDef.CostListAdjusted(StuffDef);
    foreach (var cost in costList)
    {
      Widgets.ThingIcon(new Rect(0f, curY, 20f, 20f), cost.thingDef);
      if (Map.resourceCounter.GetCount(cost.thingDef) < cost.count) GUI.color = Color.red;
      Widgets.Label(new Rect(26f, curY + 2f, 50f, 100f), cost.count.ToString());
      GUI.color = Color.white;
      string label = cost.thingDef.LabelCap;
      float height = Text.CalcHeight(label, width - 60f) - 5f;
      Widgets.Label(new Rect(60f, curY + 2f, width - 60f, height + 5f), label);
      curY += height;
    }
  }

  public override void DrawMouseAttachments()
  {
    if (useMouseIcon && icon != null)
    {
      DrawIcon(new Rect(Event.current.mousePosition.x + 8f, Event.current.mousePosition.y + 8f, 32f, 32f), null, default);
    }

    if (PlacingDef == null) return;

    Vector2 vector = Event.current.mousePosition + new Vector2(19f, 17f);
    if (useMouseIcon && icon != null) vector += new Vector2(10f, 20f);

    float curX = vector.x;
    float curY = vector.y;

    var dragger = Find.DesignatorManager.Dragger;
    int count = dragger.Dragging ? dragger.DragCells.Count() : 1;
    var costList = PlacingDef.CostListAdjusted(StuffDef);

    foreach (var cost in costList)
    {
      Widgets.ThingIcon(new Rect(curX, curY, 27f, 27f), cost.thingDef);
      int totalNeeded = count * cost.count;
      if (totalNeeded <= 0) continue;

      string text = totalNeeded.ToString();
      if (Map.resourceCounter.GetCount(cost.thingDef) < totalNeeded) GUI.color = Color.red;
      Text.Anchor = TextAnchor.MiddleLeft;
      Widgets.Label(new Rect(curX + 29f, curY, 999f, 29f), text);
      Text.Anchor = TextAnchor.UpperLeft;
      GUI.color = Color.white;
      curY += 29f;
    }
  }
}
