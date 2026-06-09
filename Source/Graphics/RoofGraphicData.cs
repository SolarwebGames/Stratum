using UnityEngine;
using Verse;

namespace SolarWeb.Stratum.Graphics;

public class RoofGraphicData
{
  public string texPath = null!;
  public Color color = Color.white;
  public DamageGraphicData? damageData;
  public bool isSeamless;
  public float skylightFrameWidth;
  public RoofEdgeGraphicData? edgeData;
  public RoofEdgeGraphicData? skylightEdgeData;
}
