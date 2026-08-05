using System;
using HarmonyLib;
using UnityEngine;
using Verse;
using global::MultiFloors;
using global::MultiFloors.Maps;

using SolarWeb.Stratum.Graphics;
using SolarWeb.Stratum.MapComponents;
using SolarWeb.Stratum.Stats;

namespace SolarWeb.Stratum.MultiFloors.Rendering;

/// <summary>
/// Paints Stratum roofs into MultiFloors' lower-level section layer.
/// </summary>
/// <remarks>
/// MultiFloors draws a single generic quad (<c>Things/Mote/TempRoof</c>) wherever its descend loop
/// finds a roof below. These entry points replace that with Stratum's own rendering, driven through
/// the shared <see cref="RoofCellPainter"/> so the art matches the current-map renderer exactly.
///
/// Anything that is not a Stratum roof -- most importantly vanilla <c>RoofConstructed</c> -- is
/// delegated back to MultiFloors' own method so its behaviour stays byte-for-byte unchanged.
/// </remarks>
public static class LowerLevelRoofPainter
{
  private static bool loggedPaintError;

  /// <summary>
  /// MultiFloors' own <c>GenerateRoofSubMeshes</c>, used as the fallback for non-Stratum roofs.
  /// </summary>
  private static readonly Action<SectionLayer_LowerLevel, IntVec3>? mfGenerateRoofSubMeshes =
    BuildMfFallback();

  private static Action<SectionLayer_LowerLevel, IntVec3>? BuildMfFallback()
  {
    try
    {
      var method = AccessTools.Method(typeof(SectionLayer_LowerLevel), "GenerateRoofSubMeshes");
      if (method == null)
      {
        StratumLog.Error(
          "MultiFloors compat: SectionLayer_LowerLevel.GenerateRoofSubMeshes not found. " +
          "Non-Stratum roofs on lower levels will not render.");
        return null;
      }
      return AccessTools.MethodDelegate<Action<SectionLayer_LowerLevel, IntVec3>>(method);
    }
    catch (Exception ex)
    {
      StratumLog.Error($"MultiFloors compat: could not bind GenerateRoofSubMeshes fallback: {ex}");
      return null;
    }
  }

  /// <summary>
  /// The render queue MultiFloors uses for its own roof quad. Read at runtime rather than
  /// hardcoded so the compat tracks MultiFloors if they retune it.
  /// </summary>
  private static int? mfRoofQueue;
  private static int MfRoofQueue
  {
    get
    {
      mfRoofQueue ??= SectionLayerUtility.RoofMaterial != null
        ? SectionLayerUtility.RoofMaterial.renderQueue
        : 2450;
      return mfRoofQueue.Value;
    }
  }

  private static RoofPaintOptions LowerLevelOptions => new()
  {
    // Match MultiFloors' queue rather than inventing one: MultiFloors has already tuned that slot
    // against the blur, snow and sand submeshes it emits into this same layer.
    RenderQueueOverride = MfRoofQueue,
    // The natural-roof edge fade only composites correctly at MetaOverlay's native queue 4500,
    // which would also put it above fog of war and lighting. Re-queued into ordinary geometry it
    // blends against a half-drawn frame and shows a bright band that shifts with the camera, so
    // paint natural roofs flat here instead.
    FlattenNaturalRoofEdges = true,
  };

  /// <summary>
  /// Replaces MultiFloors' generic roof quad for an occluding roof one or more levels down.
  /// Called from the redirected <c>GenerateRoofSubMeshes</c> call site in <c>Regenerate</c>.
  /// </summary>
  public static void PrintOpaqueRoof(SectionLayer_LowerLevel layer, IntVec3 cell, Map? lowerMap)
  {
    if (layer == null) return;

    if (lowerMap == null || !cell.InBounds(lowerMap))
    {
      PrintMfGeneric(layer, cell);
      return;
    }

    var roof = lowerMap.roofGrid?.RoofAt(cell);
    if (roof == null || !RoofStatCache.IsCustomRoof(roof) || RoofStatCache.GetGraphicData(roof) == null)
    {
      // Vanilla RoofConstructed and anything without Stratum art keeps MultiFloors' quad.
      PrintMfGeneric(layer, cell);
      return;
    }

    try
    {
      RoofCellPainter.PrintRoofCell(
        layer,
        lowerMap,
        cell,
        roof,
        lowerMap.GetComponent<RoofIntegrityGrid>(),
        AltitudeFor(),
        LowerLevelOptions);
    }
    catch (Exception ex)
    {
      if (!loggedPaintError)
      {
        loggedPaintError = true;
        StratumLog.Error($"MultiFloors compat: error painting lower-level roof at {cell}: {ex}");
      }
      PrintMfGeneric(layer, cell);
    }
  }

  /// <summary>
  /// Paints skylight glass over a lower level that has already been rendered.
  /// </summary>
  /// <remarks>
  /// Skylights are deliberately excluded from the occluding-roof test so MultiFloors keeps
  /// descending and draws the room below; the glass then goes on top. Walking from the layer's own
  /// map down to <paramref name="lowerMap"/> also covers stacked skylights on intermediate levels.
  /// </remarks>
  public static void PrintSkylightGlass(SectionLayer_LowerLevel layer, IntVec3 cell, Map? lowerMap)
  {
    if (layer == null || lowerMap == null) return;

    Map? cursor = LayerMap(layer);
    if (cursor == null) return;

    try
    {
      // Descend from the viewing level to the level that was rendered, painting any glass passed.
      while (cursor != null)
      {
        cursor = cursor.LowerMap();
        if (cursor == null) break;

        if (cell.InBounds(cursor))
        {
          var roof = cursor.roofGrid?.RoofAt(cell);
          if (roof != null && RoofStatCache.IsSkylight(roof) && RoofStatCache.IsCustomRoof(roof))
          {
            RoofCellPainter.PrintRoofCell(
              layer,
              cursor,
              cell,
              roof,
              cursor.GetComponent<RoofIntegrityGrid>(),
              AltitudeFor(),
              LowerLevelOptions);
          }
        }

        if (cursor == lowerMap) break;
      }
    }
    catch (Exception ex)
    {
      if (!loggedPaintError)
      {
        loggedPaintError = true;
        StratumLog.Error($"MultiFloors compat: error painting lower-level skylight at {cell}: {ex}");
      }
    }
  }

  /// <summary>
  /// Altitude for the lower-level roof surface.
  /// </summary>
  /// <remarks>
  /// MultiFloors draws its own quad at <see cref="AltitudeLayer.Terrain"/>, which sits below every
  /// thing it prints for the level below. Plants in adjacent unroofed cells draw graphics that
  /// spill over into roofed cells, so at that altitude they appear on top of the roof -- vanilla
  /// plants span <c>LowPlant</c>, <c>Building</c> (trees) and <c>Item</c> (decorative). Sitting
  /// just above <c>Item</c> covers all three.
  ///
  /// Deliberately kept below <c>Pawn</c> so pawns walking the transparent foundation on the level
  /// above still draw over the roof, and far below <c>Weather</c> (MultiFloors' blur submesh) and
  /// <c>LightingOverlay</c> so lower levels still darken correctly.
  /// </remarks>
  private static float AltitudeFor() => AltitudeLayer.ItemImportant.AltitudeFor();

  private static readonly AccessTools.FieldRef<MapDrawLayer, Map>? layerMapRef =
    AccessTools.FieldRefAccess<MapDrawLayer, Map>("map");

  private static Map? LayerMap(SectionLayer_LowerLevel layer)
  {
    try { return layerMapRef?.Invoke(layer); }
    catch { return null; }
  }

  private static void PrintMfGeneric(SectionLayer_LowerLevel layer, IntVec3 cell)
  {
    if (mfGenerateRoofSubMeshes != null)
    {
      mfGenerateRoofSubMeshes(layer, cell);
      return;
    }

    // Last resort if MultiFloors renamed the method: reproduce its quad inline so the cell is
    // never left blank.
    if (SectionLayerUtility.RoofMaterial != null)
    {
      Printer_Plane.PrintPlane(layer, cell.ToVector3ShiftedWithAltitude(AltitudeLayer.Terrain), Vector2.one, SectionLayerUtility.RoofMaterial);
    }
  }
}
