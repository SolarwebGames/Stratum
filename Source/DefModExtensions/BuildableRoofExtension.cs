using UnityEngine;
using Verse;
using SolarWeb.Stratum.Graphics;

namespace SolarWeb.Stratum.DefModExtensions;

public class BuildableRoofExtension : DefModExtension
{
  public ThingDef? buildableDef;
  public DesignationCategoryDef? designationCategory;

  public RoofGraphicData? graphicData;
  public bool isSkylight = false;
  public float transparency = 0f; // 0.0 = opaque, 1.0 = clear
  public Color? glassTint;
  public float skylightFrameWidth = 0.1f;
  public float solarEfficiency = 0f; // 0.0 = none, 1.0 = standard solar cell

  public float thermalConductivity = 0.1f; // 0.1 = standard thin roof equivalent
  public float stuffInsulationMultiplier = 1f;
  public bool isAirtight = true;

  public float damageThreshold = 0f;
  public float armorRating = 0f;
  public float stuffArmorMultiplier = 1f;

  public bool isRetractable = false;
  public int transitionTicksPerRing = 30;
}
