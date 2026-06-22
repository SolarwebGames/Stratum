using System.Collections.Generic;
using System.Text;
using RimWorld;
using UnityEngine;
using Verse;

using SolarWeb.Stratum.Stats;
using SolarWeb.Stratum.WorldComponents;

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
    Find.World.GetComponent<RoofSelectionPool>()?.Return(this);
  }

  public IEnumerable<Gizmo> GetGizmos()
  {
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
  }

  public string GetInspectString()
  {
    StringBuilder sb = new();

    var integrity = map.GetComponent<MapComponents.RoofIntegrityGrid>();
    var solarComp = map.GetComponent<MapComponents.SolarRoofMapComponent>();
    if (solarComp != null && solarComp.TryGetSolarNetworkPower(cell, out var cellPower, out var netPower))
    {
      sb.AppendLine("Stratum_SolarPower_Cell".Translate(cellPower.currentPower.ToString("F0"), cellPower.maxPower.ToString("F0")));
      sb.AppendLine("Stratum_SolarPower_Grid".Translate(netPower.currentPower.ToString("F0"), netPower.maxPower.ToString("F0")));
    }

    float beauty = RoofStatCache.GetBeauty(def, integrity?.GetStuff(cell));
    if (beauty != 0) sb.AppendLine("Beauty_Label".Translate() + ": " + beauty.ToString("F2"));

    float transparency = RoofStatCache.GetTransparency(def);
    if (transparency > 0) sb.AppendLine("Stratum_Transparency".Translate() + ": " + transparency.ToStringPercent());

    float solarEff = RoofStatCache.GetSolarEfficiency(def);
    if (solarEff > 0) sb.AppendLine("Stratum_SolarEfficiency".Translate() + ": " + solarEff.ToStringPercent());

    float conductivity = RoofStatCache.GetThermalConductivity(def, integrity?.GetStuff(cell));
    float insulation = 1f - conductivity;
    string insulationStr = insulation.ToString("P1");
    sb.AppendLine(DefOf.StatDefOf.Insulation.LabelCap + ": " + insulationStr);

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
