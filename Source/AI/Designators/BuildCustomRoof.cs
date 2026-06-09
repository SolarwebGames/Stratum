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

namespace SolarWeb.Stratum.AI.Designators;

public class BuildCustomRoof : Designator_Cells
{
  private readonly RoofDef roofDef;
  private readonly BuildableRoofExtension ext;
  private ThingDef? stuffDef;
  private bool writeStuff;

  public override DrawStyleCategoryDef DrawStyleCategory => DrawStyleCategoryDefOf.Areas;

  protected override DesignationDef Designation => null!;

  public BuildableDef PlacingDef => ext.buildableDef!;

  private int GetAccessibleStuffCount(ThingDef stuff)
  {
    int count = 0;
    foreach (var t in Map.listerThings.ThingsOfDef(stuff))
    {
      if (!t.IsForbidden(Faction.OfPlayer))
      {
        count += t.stackCount;
      }
    }
    return count;
  }

  public ThingDef? StuffDef
  {
    get
    {
      if (stuffDef != null) return stuffDef;
      if (PlacingDef is ThingDef { MadeFromStuff: true } thingDef && Map != null)
      {
        ThingDef? cheapestStuff = null;
        float minVal = float.MaxValue;

        foreach (var stuff in GenStuff.AllowedStuffsFor(thingDef))
        {
          if (ext.allowedStuff != null && !ext.allowedStuff.Contains(stuff)) continue;
          
          if (GetAccessibleStuffCount(stuff) >= thingDef.CostStuffCount)
          {
            float val = stuff.BaseMarketValue;
            if (val < minVal)
            {
              minVal = val;
              cheapestStuff = stuff;
            }
          }
        }

        if (cheapestStuff != null)
        {
          stuffDef = cheapestStuff;
        }
        else
        {
          stuffDef = GenStuff.DefaultStuffFor(thingDef);
          if (stuffDef != null && ext.allowedStuff != null && !ext.allowedStuff.Contains(stuffDef))
            stuffDef = ext.allowedStuff.FirstOrDefault();
        }
      }
      return stuffDef;
    }
  }

  public override string Label
  {
    get
    {
      if (PlacingDef is ThingDef thingDef && writeStuff && stuffDef != null)
        return GenLabel.ThingLabel(thingDef, stuffDef).CapitalizeFirst();
      return base.Label;
    }
  }

  public override Color IconDrawColor
  {
    get
    {
      var stuff = StuffDef;
      if (stuff != null)
      {
        return PlacingDef.GetColorForStuff(stuff);
      }
      return defaultIconColor;
    }
  }

  public BuildCustomRoof(RoofDef roofDef, BuildableRoofExtension ext)
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

    // Use atlased texture for icon if available
    if (gd != null && RoofAtlasManager.TryGetUv(gd.texPath, out var uvs, out var mat))
    {
      icon = mat!.mainTexture;
      if (uvs != null && uvs.Length >= 4)
      {
        // UVs are BL, TL, TR, BR
        float minU = uvs[0].x;
        float minV = uvs[0].y;
        float maxU = uvs[2].x;
        float maxV = uvs[2].y;
        iconTexCoords = new Rect(minU, minV, maxU - minU, maxV - minV);
      }
    }

    soundDragSustain = SoundDefOf.Designate_DragStandard;
    soundDragChanged = SoundDefOf.Designate_DragStandard_Changed;
    soundSucceeded = SoundDefOf.Designate_ZoneAdd_Roof;
    useMouseIcon = true;
  }

  public override void DrawIcon(Rect rect, Material? buttonMat, GizmoRenderParms parms)
  {
    if (icon == null) return;

    var gd = RoofStatCache.GetGraphicData(roofDef);
    Material? mat = overrideMaterial ?? buttonMat;

    // If it's a framed skylight, we 9-slice the icon to show frame + glass
    if (gd != null && RoofStatCache.IsSkylight(roofDef) && gd.skylightFrameWidth > 0f)
    {
      float f = gd.skylightFrameWidth;
      float glassAlpha = 1f - RoofStatCache.GetTransparency(roofDef);

      Color frameColor = IconDrawColor;
      frameColor.a = 1f;

      Color glassColor = gd.color;
      glassColor.a = glassAlpha;

      if (parms.lowLight)
      {
        frameColor.a *= 0.6f;
        glassColor.a *= 0.6f;
      }

      Rect tc = iconTexCoords;
      float[] rx = { rect.xMin, rect.xMin + rect.width * f, rect.xMin + rect.width * (1f - f), rect.xMax };
      float[] ry = { rect.yMin, rect.yMin + rect.height * f, rect.yMin + rect.height * (1f - f), rect.yMax };

      // UVs: y loop 0 is top rect slice -> UV slice index 2 to 3
      float[] ux = { tc.xMin, tc.xMin + tc.width * f, tc.xMin + tc.width * (1f - f), tc.xMax };
      float[] uy = { tc.yMin, tc.yMin + tc.height * f, tc.yMin + tc.height * (1f - f), tc.yMax };

      for (int x = 0; x < 3; x++)
      {
        for (int y = 0; y < 3; y++)
        {
          bool isCenter = (x == 1 && y == 1);
          GUI.color = isCenter ? glassColor : frameColor;

          Rect qRect = new Rect(rx[x], ry[y], rx[x + 1] - rx[x], ry[y + 1] - ry[y]);
          Rect qUv = new Rect(ux[x], uy[2 - y], ux[x + 1] - ux[x], uy[2 - y + 1] - uy[2 - y]);

          GUI.DrawTextureWithTexCoords(qRect, (Texture2D)icon, qUv);
        }
      }
      GUI.color = Color.white;
    }
    else
    {
      GUI.color = IconDrawColor;
      if (parms.lowLight) GUI.color = GUI.color.ToTransparent(0.6f);

      Widgets.DrawTextureFitted(rect, icon, iconDrawScale * 0.85f, iconProportions, iconTexCoords, iconAngle, mat);
      GUI.color = Color.white;
    }
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
            
            if (GetAccessibleStuffCount(stuff) > 0)
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
      var list = new List<FloatMenuOption>();
      foreach (var item in GenStuff.AllowedStuffsFor(thingDef))
      {
        if ((ext.allowedStuff == null || ext.allowedStuff.Contains(item)) && (DebugSettings.godMode || GetAccessibleStuffCount(item) > 0))
        {
          var localStuffDef = item;
          string str = GenLabel.ThingLabel(thingDef, localStuffDef).CapitalizeFirst();
          var floatMenuOption = new FloatMenuOption(str, delegate
          {
            base.ProcessInput(ev);
            Find.DesignatorManager.Select(this);
            stuffDef = localStuffDef;
            writeStuff = true;
          }, item);
          list.Add(floatMenuOption);
        }
      }

      if (list.Count == 0)
      {
        Messages.Message("NoStuffsToBuildWith".Translate(), MessageTypeDefOf.RejectInput, historical: false);
        return;
      }

      var floatMenu = new FloatMenu(list) { onCloseCallback = () => writeStuff = true };
      Find.WindowStack.Add(floatMenu);
      Find.DesignatorManager.Select(this);
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
      Map.GetComponent<RoofIntegrityGrid>()?.InitializeRoof(c, roofDef, StuffDef);
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

    tracker.AddRecord(c, roofDef, workToBuild, StuffDef);
    var frame = (RoofFrame)ThingMaker.MakeThing(DefOf.ThingDefOf.RoofFrame);
    frame.targetRoofDef = roofDef;
    frame.targetRoofStuff = StuffDef;
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
