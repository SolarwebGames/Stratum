using System.Collections.Generic;
using System.Text;
using RimWorld;
using UnityEngine;
using Verse;

using SolarWeb.Stratum.Stats;
using SolarWeb.Stratum.WorldComponents;
using SolarWeb.Stratum.DefModExtensions;
using SolarWeb.Stratum.Graphics;

namespace SolarWeb.Stratum.UI;

public class SelectedRoof : ISelectable, IRenameable, ICancelableByDesignator
{
  public Map map = null!;
  public IntVec3 cell;
  public RoofDef def = null!;

  public string RenamableLabel
  {
    get => Label;
    set { }
  }

  public string BaseLabel => def.label;
  public string InspectLabel => Label;

  public string Label
  {
    get
    {
      var stuff = map.GetComponent<MapComponents.RoofIntegrityGrid>()?.GetStuff(cell);
      if (stuff != null) return "ThingMadeOfStuffLabel".Translate(stuff.LabelAsStuff, def.label).CapitalizeFirst();
      return def.LabelCap;
    }
  }

  public bool CanCancel => map.areaManager.NoRoof[cell];

  public SelectedRoof(Map map, IntVec3 cell, RoofDef def)
  {
    Initialize(map, cell, def);
  }

  public void Initialize(Map map, IntVec3 cell, RoofDef def)
  {
    this.map = map;
    this.cell = cell;
    this.def = def;
  }

  public void Dispose()
  {
    RoofSelectionTracker.Instance.ClearSelectTimeFor(this);
    Find.World.GetComponent<RoofSelectionPool>()?.Return(this);
  }

  public IEnumerable<Gizmo> GetGizmos()
  {
    var ext = def.GetModExtension<BuildableRoofExtension>();
    if (ext != null && BuildableRoofGenerator.RoofToDesignator.TryGetValue(def, out var designator) && designator.Visible)
    {
      var stuff = map.GetComponent<MapComponents.RoofIntegrityGrid>()?.GetStuff(cell);
      var tint = map.GetComponent<MapComponents.RoofIntegrityGrid>()?.GetGlassTint(cell);

      Color defaultIconColor = Color.white;
      var bDef = ext.buildableDef;
      var gd = RoofStatCache.GetGraphicData(def);
      if (bDef != null)
      {
        if (gd != null)
        {
          defaultIconColor = gd.color;
        }
        else if (bDef.graphicData != null)
        {
          defaultIconColor = bDef.graphicData.color;
        }
      }
      
      yield return new Command_BuildCopyRoof
      {
        defaultLabel = "CommandBuildCopy".Translate(),
        defaultDesc = "CommandBuildCopyDesc".Translate(),
        icon = designator.icon,
        iconTexCoords = designator.iconTexCoords,
        hotKey = KeyBindingDefOf.Misc11,
        roofDef = def,
        stuffDef = stuff,
        selectedTint = tint,
        defaultIconColor = defaultIconColor,
        action = delegate
        {
          if (stuff != null)
          {
            designator.SetStuffDef(stuff);
          }
          if (RoofStatCache.IsSkylight(def))
          {
            designator.SelectedTint = tint;
          }
          Find.DesignatorManager.Select(designator);
        }
      };
    }

    var disabled = def?.isThickRoof == true || map.areaManager.NoRoof[cell];
    var disabledReason = def?.isThickRoof == true ? "MessageNothingCanRemoveThickRoofs".Translate()
      : map.areaManager.NoRoof[cell] ? "Stratum_AlreadyRemoving".Translate() : null;

    yield return new Command_Action
    {
      defaultLabel = "DesignatorDeconstruct".Translate(),
      defaultDesc = "DesignatorDeconstructDesc".Translate(),
      icon = ContentFinder<Texture2D>.Get("UI/Designators/Deconstruct"),
      Disabled = disabled,
      disabledReason = disabledReason,
      action = delegate
      {
        if (def?.isThickRoof == true) return;

        map.areaManager.NoRoof[cell] = true;
      },
      hotKey = KeyBindingDefOf.Designator_Deconstruct
    };

    if (map.areaManager.NoRoof[cell])
    {
      yield return new Command_Action
      {

        defaultLabel = "Stratum_CancelRoofRemoval".Translate(),
        defaultDesc = "Stratum_CancelRoofRemovalDesc".Translate(),
        icon = ContentFinder<Texture2D>.Get("UI/Designators/Cancel"),
        hotKey = KeyBindingDefOf.Designator_Cancel,
        action = delegate
        {
          map.areaManager.NoRoof[cell] = false;
        }
      };
    }

    if (def == RimWorld.RoofDefOf.RoofRockThin || def == RimWorld.RoofDefOf.RoofRockThick)
    {
      var designation = map.designationManager.DesignationAt(cell, DefOf.DesignationDefOf.SmoothRoof);
      if (designation != null)
      {
        yield return new Command_Action
        {
          defaultLabel = "SolarWeb_Stratum_CancelSmoothRoof".Translate(),
          defaultDesc = "SolarWeb_Stratum_CancelSmoothRoofDesc".Translate(),
          icon = ContentFinder<Texture2D>.Get("UI/Designators/Cancel"),
          action = delegate
          {
            map.designationManager.TryRemoveDesignation(cell, DefOf.DesignationDefOf.SmoothRoof);
          },
          hotKey = KeyBindingDefOf.Designator_Cancel
        };
      }
      else
      {
        yield return new Command_Action
        {
          defaultLabel = "SolarWeb_Stratum_DesignatorSmoothRoof".Translate(),
          defaultDesc = "SolarWeb_Stratum_DesignatorSmoothRoofDesc".Translate(),
          icon = ContentFinder<Texture2D>.Get("UI/Designators/SmoothSurface"),
          action = delegate
          {
            map.designationManager.AddDesignation(new Designation(cell, DefOf.DesignationDefOf.SmoothRoof));
          }
        };
      }
    }
  }

  public string GetInspectString()
  {
    StringBuilder sb = new();

    var solarComp = map.GetComponent<MapComponents.SolarRoofMapComponent>();
    if (solarComp != null && solarComp.TryGetSolarNetworkPower(cell, out var cellPower, out var netPower))
    {
      sb.AppendLine("Stratum_SolarPower_Cell".Translate(cellPower.currentPower.ToString("F0"), cellPower.maxPower.ToString("F0")));
      sb.AppendLine("Stratum_SolarPower_Grid".Translate(netPower.currentPower.ToString("F0"), netPower.maxPower.ToString("F0")));
    }

    float solarOut = RoofStatCache.GetSolarOutput(def);
    if (solarOut > 0) sb.AppendLine(DefOf.StatDefOf.SolarOutput.LabelCap + ": " + solarOut.ToString("F1") + " W");

    return sb.ToString().TrimEndNewlines();
  }

  public IEnumerable<InspectTabBase> GetInspectTabs() => null!;

  public override bool Equals(object? obj)
  {
    return obj is SelectedRoof other && other.map == map && other.cell == cell;
  }

  public override int GetHashCode()
  {
    return map.GetHashCode() ^ cell.GetHashCode();
  }

  public void CancelByDesignator()
  {
    if (map.areaManager.NoRoof[cell])
    {
      map.areaManager.NoRoof[cell] = false;
    }
  }
}

public class Command_BuildCopyRoof : Command_Action
{
  public RoofDef roofDef = null!;
  public ThingDef? stuffDef;
  public Color? selectedTint;

  public override void DrawIcon(Rect rect, Material? buttonMat, GizmoRenderParms parms)
  {
    RoofIconUtility.DrawDesignatorIcon(rect, roofDef, stuffDef, selectedTint, defaultIconColor, icon, iconTexCoords, buttonMat, parms);
  }
}
