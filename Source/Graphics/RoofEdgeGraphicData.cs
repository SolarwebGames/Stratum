using UnityEngine;
using Verse;
using EdgeType = RimWorld.SectionLayer_TerrainEdges.EdgeType;

namespace SolarWeb.Stratum.Graphics;

public class RoofEdgeGraphicData
{
  [NoTranslate] public string? OShapeTexPath;
  [NoTranslate] public string? UShapeTexPath;
  [NoTranslate] public string? CornerInnerTexPath;
  [NoTranslate] public string? CornerOuterTexPath;
  [NoTranslate] public string? FlatTexPath;

  public ShaderTypeDef? shaderType;
  public Color color = Color.white;

  public bool HasAnyTexture =>
    !FlatTexPath.NullOrEmpty() || !CornerOuterTexPath.NullOrEmpty() ||
    !CornerInnerTexPath.NullOrEmpty() || !UShapeTexPath.NullOrEmpty() || !OShapeTexPath.NullOrEmpty();

  public string? GetTexPath(EdgeType type)
  {
    return type switch
    {
      EdgeType.OShape => OShapeTexPath,
      EdgeType.UShape => UShapeTexPath,
      EdgeType.CornerInner => CornerInnerTexPath,
      EdgeType.CornerOuter => CornerOuterTexPath,
      EdgeType.Flat => FlatTexPath,
      _ => null
    };
  }
}
