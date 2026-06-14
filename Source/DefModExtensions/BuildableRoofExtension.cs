using System.Collections.Generic;
using Verse;

using SolarWeb.Stratum.Graphics;
using UnityEngine;

namespace SolarWeb.Stratum.DefModExtensions;

public class BuildableRoofExtension : DefModExtension
{
  public ThingDef? buildableDef;
  public DesignationCategoryDef? designationCategory;

  public RoofGraphicData? graphicData;
  public bool isSkylight = false;
  public float transparency = 0f; // 0.0 = opaque, 1.0 = clear
  public Color? glassTint;
  public float solarEfficiency = 0f; // 0.0 = none, 1.0 = standard solar cell

  public float thermalConductivity = 0.1f; // 0.1 = standard thin roof equivalent
  public float stuffInsulationMultiplier = 1f;
  public List<ThingDef>? allowedStuff;
}
