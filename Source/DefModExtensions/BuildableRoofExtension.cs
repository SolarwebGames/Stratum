using System.Collections.Generic;
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

  public bool isAirtight = true;

  public bool isRetractable = false;

  public bool allowHangingAttachments = true;
  public bool allowRooftopAttachments = true;
  public readonly Dictionary<TerrainDef, ThingDef> terrainToStuff = [];
}
