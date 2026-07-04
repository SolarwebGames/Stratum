using RimWorld;
using UnityEngine;
using Verse;

using SolarWeb.Stratum.Stats;
using SolarWeb.Stratum.MapComponents;

namespace SolarWeb.Stratum.Graphics;

[StaticConstructorOnStartup]
public class LightPoolRenderer : SectionLayer
{
  private static Material? poolMat;
  public static Material PoolMat
  {
    get
    {
      if (poolMat == null)
      {
        poolMat = MaterialPool.MatFrom(RimWorldTextures.Things.Mote.FireGlow, ShaderDatabase.Transparent);
      }
      return poolMat;
    }
  }

  public LightPoolRenderer(Section section) : base(section)
  {
    relevantChangeTypes = (ulong)MapMeshFlagDefOf.Roofs;
  }

  public override bool Visible => true;

  public override void DrawLayer()
  {
    if (!Visible) return;

    var map = Map;
    if (map == null || map.skyManager == null) return;

    float skyGlow = map.skyManager.CurSkyGlow;
    if (skyGlow <= 0.01f) return;

    LayerSubMesh subMesh = GetSubMesh(PoolMat);
    if (subMesh == null || subMesh.verts.Count == 0 || !subMesh.finalized || subMesh.disabled) return;

    var propertyBlock = new MaterialPropertyBlock();
    propertyBlock.SetColor("_Color", new Color(skyGlow, skyGlow, skyGlow, skyGlow));

    UnityEngine.Graphics.DrawMesh(subMesh.mesh, Matrix4x4.identity, PoolMat, subMesh.renderLayer, null, 0, propertyBlock);
  }

  public override void Regenerate()
  {
    ClearSubMeshes(MeshParts.All);

    Map map = base.Map;
    if (map == null || map.roofGrid == null) return;
    RoofGrid roofGrid = map.roofGrid;
    CellRect cellRect = section.CellRect;

    bool isCutscene = false;
    CellRect captureBounds = default;
    if (ModsConfig.OdysseyActive)
    {
      isCutscene = WorldComponent_GravshipController.CutsceneInProgress && !GravshipCapturer.IsGravshipRenderInProgress && map == Find.CurrentMap;
      captureBounds = GravshipCapturer.GravshipCaptureBounds;
    }

    float y = AltitudeLayer.Floor.AltitudeFor() + 0.01f;

    LayerSubMesh subMesh = GetSubMesh(PoolMat);
    if (subMesh == null) return;

    var skylightDirt = map.GetComponent<SkylightCoating>();

    foreach (IntVec3 c in cellRect)
    {
      if (isCutscene && captureBounds.Contains(c)) continue;

      RoofDef roof = roofGrid.RoofAt(c);
      if (roof == null || !RoofStatCache.IsSkylight(roof)) continue;

      float transparency = RoofStatCache.GetTransparency(roof);
      if (skylightDirt != null)
      {
        float opacity = Mathf.Clamp01(skylightDirt.GetDirtLevel(c) + skylightDirt.GetSnowLevel(c));
        transparency *= (1f - opacity);
      }
      if (transparency <= 0f) continue;

      Color glassColor = RoofStatCache.GetColor(roof);

      float alpha = transparency * 0.4f;
      Color32 finalColor = new(
        (byte)(glassColor.r * 255),
        (byte)(glassColor.g * 255),
        (byte)(glassColor.b * 255),
        (byte)(alpha * 255)
      );

      int vCount = subMesh.verts.Count;

      subMesh.verts.Add(new Vector3(c.x, y, c.z));
      subMesh.verts.Add(new Vector3(c.x, y, c.z + 1));
      subMesh.verts.Add(new Vector3(c.x + 1, y, c.z + 1));
      subMesh.verts.Add(new Vector3(c.x + 1, y, c.z));

      for (int i = 0; i < 4; i++) subMesh.colors.Add(finalColor);

      subMesh.uvs.Add(new Vector2(0f, 0f));
      subMesh.uvs.Add(new Vector2(0f, 1f));
      subMesh.uvs.Add(new Vector2(1f, 1f));
      subMesh.uvs.Add(new Vector2(1f, 0f));

      subMesh.tris.Add(vCount);
      subMesh.tris.Add(vCount + 1);
      subMesh.tris.Add(vCount + 2);
      subMesh.tris.Add(vCount);
      subMesh.tris.Add(vCount + 2);
      subMesh.tris.Add(vCount + 3);
    }

    FinalizeMesh(MeshParts.All);
  }
}
