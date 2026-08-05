using RimWorld;
using UnityEngine;
using Verse;

using SolarWeb.Stratum.Hooks;
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

  public override bool Visible => Stratum.Settings.enableSkylightLighting;

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
    if (!Stratum.Settings.enableSkylightLighting) return;

    Map map = Map;
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

    // Hoisted: the Map overload of GetEffectiveTransparency does a GetComponent list scan, and the
    // Has*Handlers calls are two dictionary lookups each -- neither belongs in a per-cell loop.
    var coating = map.GetComponent<MapComponents.SkylightCoating>();
    var integrity = map.GetComponent<MapComponents.RoofIntegrityGrid>();
    bool applyTransmissionHook = MapHookRegistry.HasSkylightTransmissionHandlers(map);
    bool applyTintHook = MapHookRegistry.HasSkylightTintHandlers(map);

    foreach (IntVec3 c in cellRect)
    {
      if (isCutscene && captureBounds.Contains(c)) continue;

      RoofDef roof = roofGrid.RoofAt(c);
      if (roof == null || !RoofStatCache.IsSkylight(roof)) continue;

      Building edifice = c.GetEdifice(map);
      if (edifice != null && edifice.def.staticSunShadowHeight > 0f) continue;

      float transparency = RoofStatCache.GetEffectiveTransparency(roof, coating, c);
      if (transparency <= 0f) continue;

      // Identity base: the hook reports purely what the levels above do to this cell, so an
      // unsubscribed map multiplies by exactly 1 and is unchanged.
      if (applyTransmissionHook)
      {
        transparency *= MapHookRegistry.GetCellSkylightTransmission(map, c, 1f);
        if (transparency <= 0.001f) continue;
      }

      // The pool is the light *through* the glass, so it takes the glass tint -- which also picks
      // up a player-painted per-cell tint. Every other consumer already used GetGlassTint; this
      // renderer was the one holdout on GetColor (the roof texture's colour).
      Color glassColor = RoofStatCache.GetGlassTint(roof, integrity, c);
      if (applyTintHook) glassColor *= MapHookRegistry.GetCellSkylightTint(map, c, Color.white);

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
