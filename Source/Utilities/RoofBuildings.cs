using System;
using UnityEngine;
using Verse;
using RimWorld;

using SolarWeb.Stratum.DefModExtensions;
using SolarWeb.Stratum.Hooks;

namespace SolarWeb.Stratum.Utilities;

[StaticConstructorOnStartup]
public static class RoofBuildings
{
  public static bool showRoofBuildings = false;
  public static bool isDeconstructingRoof = false;

  private static Texture2D? showRoofBuildingsIcon;
  public static Texture2D ShowRoofBuildingsIcon => showRoofBuildingsIcon ??= CreateDefaultToggleIcon();

  public static bool ShouldRenderRoofBuilding(Thing t)
  {
    if (t == null || t.def == null) return false;
    var map = t.Map;
    if (map != null)
    {
      var registry = MapHookRegistry.Get(map);
      if (registry != null)
      {
        var handlers = registry.GetHandlers<MapHookRegistry.RoofBuildingRenderCheckHandler>(MapHookRegistry.HookId.RoofBuildingRenderCheck);
        if (handlers != null)
        {
          for (int i = 0; i < handlers.Count; i++)
          {
            try
            {
              var res = handlers[i](t);
              if (res.HasValue) return res.Value;
            }
            catch (System.Exception ex)
            {
              StratumLog.Error($"Error in RoofBuildingRenderCheck subscriber: {ex}");
            }
          }
        }
      }
    }

    var attachmentType = GetAttachmentType(t);
    if (attachmentType == RoofAttachmentType.Rooftop)
    {
      return Find.PlaySettings != null && (Find.PlaySettings.showRoofOverlay || IsPlacingOrDesignatingRoofs(t.Map) || t is Blueprint || t is Frame);
    }
    else
    {
      return showRoofBuildings || IsPlacingOrDesignatingRoofs(t.Map);
    }
  }

  public static bool IsPlacingOrDesignatingRoofs(Map map)
  {
    if (map == null || Find.DesignatorManager == null) return false;
    var designator = Find.DesignatorManager.SelectedDesignator;
    if (designator != null)
    {
      if (designator is Designator_Build buildDesignator)
      {
        var entDef = buildDesignator.PlacingDef;
        if (entDef is ThingDef thingDef && thingDef.HasModExtension<RoofBuilding>())
        {
          return true;
        }
      }
      if (designator is AI.Designators.BuildCustomRoof)
      {
        return true;
      }
      if (designator is Designator_AreaNoRoof || designator is Designator_AreaBuildRoof)
      {
        return true;
      }
    }
    return false;
  }

  private static bool?[] isRoofBuildingCache = new bool?[ushort.MaxValue + 1];

  public static bool IsRoofBuildingOrBlueprintOrFrame(Thing t)
  {
    if (t == null || t.def == null) return false;

    ushort hash = t.def.shortHash;
    bool? cached = isRoofBuildingCache[hash];
    if (cached.HasValue) return cached.Value;

    bool result = false;
    if (t.def.HasModExtension<RoofBuilding>()) result = true;
    else if (t is Blueprint blueprint && blueprint.def != null && blueprint.def.entityDefToBuild is ThingDef buildDef && buildDef.HasModExtension<RoofBuilding>()) result = true;
    else if (t is Frame frame && frame.def != null && frame.def.entityDefToBuild is ThingDef frameDef && frameDef.HasModExtension<RoofBuilding>()) result = true;

    isRoofBuildingCache[hash] = result;
    return result;
  }

  private static RoofAttachmentType?[] attachmentTypeCache = new RoofAttachmentType?[ushort.MaxValue + 1];

  public static RoofAttachmentType GetAttachmentType(Thing t)
  {
    if (t == null || t.def == null) return RoofAttachmentType.Hanging;

    ushort hash = t.def.shortHash;
    var cached = attachmentTypeCache[hash];
    if (cached.HasValue) return cached.Value;

    RoofAttachmentType type = RoofAttachmentType.Hanging;
    var ext = t.def.GetModExtension<RoofBuilding>();
    if (ext != null) type = ext.attachmentType;
    else if (t is Blueprint blueprint && blueprint.def != null && blueprint.def.entityDefToBuild is ThingDef buildDef)
    {
      var buildExt = buildDef.GetModExtension<RoofBuilding>();
      if (buildExt != null) type = buildExt.attachmentType;
    }
    else if (t is Frame frame && frame.def != null && frame.def.entityDefToBuild is ThingDef frameDef)
    {
      var frameExt = frameDef.GetModExtension<RoofBuilding>();
      if (frameExt != null) type = frameExt.attachmentType;
    }

    attachmentTypeCache[hash] = type;
    return type;
  }

  public static RoofAttachmentType GetAttachmentType(BuildableDef def)
  {
    if (def == null) return RoofAttachmentType.Hanging;
    if (def is ThingDef thingDef)
    {
      if (thingDef.entityDefToBuild is ThingDef buildDef)
      {
        thingDef = buildDef;
      }
      var ext = thingDef.GetModExtension<RoofBuilding>();
      if (ext != null) return ext.attachmentType;
    }
    return RoofAttachmentType.Hanging;
  }


  public static bool HasRoofBuildingAt(Map map, IntVec3 cell)
  {
    if (map == null || !cell.InBounds(map)) return false;
    var thingList = cell.GetThingList(map);
    if (thingList == null) return false;
    for (int i = 0; i < thingList.Count; i++)
    {
      if (IsRoofBuildingOrBlueprintOrFrame(thingList[i]))
      {
        return true;
      }
    }
    return false;
  }

  public static bool IsRoofValidForExistingBuildings(RoofDef roofDef, Map map, IntVec3 cell)
  {
    if (roofDef == null || map == null || !cell.InBounds(map)) return true;
    var thingList = cell.GetThingList(map);
    if (thingList == null) return true;

    for (int i = 0; i < thingList.Count; i++)
    {
      var thing = thingList[i];
      if (IsRoofBuildingOrBlueprintOrFrame(thing))
      {
        var attachmentType = GetAttachmentType(thing);
        if (roofDef.isNatural && attachmentType == RoofAttachmentType.Rooftop)
        {
          return false;
        }

        var roofExt = roofDef.GetModExtension<BuildableRoofExtension>();
        if (roofExt != null)
        {
          if (attachmentType == RoofAttachmentType.Hanging && !roofExt.allowHangingAttachments)
          {
            return false;
          }
          if (attachmentType == RoofAttachmentType.Rooftop && !roofExt.allowRooftopAttachments)
          {
            return false;
          }
        }
      }
    }
    return true;
  }

  public static bool HasNonMinifiableRoofBuildingAt(Map map, IntVec3 cell)
  {
    if (map == null || !cell.InBounds(map)) return false;
    var thingList = cell.GetThingList(map);
    if (thingList == null) return false;
    for (int i = 0; i < thingList.Count; i++)
    {
      var t = thingList[i];
      if (t != null && t.def != null && t is Building && t.def.HasModExtension<RoofBuilding>() && !t.def.Minifiable)
      {
        return true;
      }
    }
    return false;
  }

  public static bool HasConstructedRoofBuildingAt(Map map, IntVec3 cell)
  {
    if (map == null || !cell.InBounds(map)) return false;
    var thingList = cell.GetThingList(map);
    if (thingList == null) return false;
    for (int i = 0; i < thingList.Count; i++)
    {
      var t = thingList[i];
      if (t != null && t.def != null && t is Building && t.def.HasModExtension<RoofBuilding>())
      {
        return true;
      }
    }
    return false;
  }


  public static void DirtyAllRoofBuildingCells(Map map)
  {
    if (map?.mapDrawer == null || map.listerThings?.AllThings == null) return;
    foreach (var thing in map.listerThings.AllThings)
    {
      if (IsRoofBuildingOrBlueprintOrFrame(thing))
      {
        foreach (var cell in thing.OccupiedRect())
        {
          map.mapDrawer.MapMeshDirty(cell, MapMeshFlagDefOf.Things);
        }
      }
    }
  }

  public static void HandleRoofLoss(Map map, IntVec3 cell)
  {
    if (map == null) return;
    var thingList = cell.GetThingList(map);
    if (thingList == null) return;
    for (int i = thingList.Count - 1; i >= 0; i--)
    {
      var thing = thingList[i];
      if (thing != null && thing.def != null && thing.def.HasModExtension<RoofBuilding>())
      {
        if (isDeconstructingRoof)
        {
          if (thing.def.Minifiable)
          {
            thing.Uninstall();
          }
          else
          {
            thing.Destroy(DestroyMode.Deconstruct);
          }
        }
        else
        {
          thing.Destroy(DestroyMode.KillFinalize);
        }
      }
    }
  }

  private static Texture2D CreateDefaultToggleIcon()
  {
    var customTex = ContentFinder<Texture2D>.Get("UI/Buttons/ShowRoofBuildings", false);
    if (customTex != null) return customTex;

    Texture2D tex = new Texture2D(32, 32, TextureFormat.RGBA32, false);
    for (int x = 0; x < 32; x++)
    {
      for (int y = 0; y < 32; y++)
      {
        tex.SetPixel(x, y, Color.clear);
      }
    }
    for (int x = 4; x <= 28; x++)
    {
      int targetY = 24 - Math.Abs(x - 16);
      tex.SetPixel(x, targetY, Color.white);
      tex.SetPixel(x, targetY - 1, Color.white);
    }
    for (int x = 12; x <= 20; x++)
    {
      for (int y = 25; y <= 29; y++)
      {
        if (x == 12 || x == 20 || y == 29)
        {
          tex.SetPixel(x, y, Color.white);
        }
      }
    }
    tex.Apply();
    return tex;
  }

  public static bool? CheckBlocksConstruction(Thing constructible, Thing existingThing)
  {
    if (constructible == null || existingThing == null || constructible.def == null || existingThing.def == null) return null;
    if (existingThing == constructible) return null;

    var defConstructible = constructible.def.entityDefToBuild as ThingDef ?? constructible.def;
    var defExisting = existingThing.def;

    bool isConstructibleRoofBuilding = defConstructible != null && defConstructible.HasModExtension<RoofBuilding>();
    bool isExistingRoofBuilding = defExisting.HasModExtension<RoofBuilding>();

    if (isConstructibleRoofBuilding && isExistingRoofBuilding)
    {
      var typeConstructible = GetAttachmentType(constructible);
      var typeExisting = GetAttachmentType(existingThing);

      if (typeConstructible == typeExisting)
      {
        return true;
      }
      else
      {
        return false;
      }
    }

    if (isConstructibleRoofBuilding || isExistingRoofBuilding)
    {
      var roofDef = isConstructibleRoofBuilding ? defConstructible : defExisting;
      var ext = roofDef?.GetModExtension<RoofBuilding>();
      var attachmentType = ext != null ? ext.attachmentType : RoofAttachmentType.Hanging;
      var floorDef = isConstructibleRoofBuilding ? defExisting : defConstructible;

      if (floorDef != null)
      {
        bool isImpassable = floorDef.IsEdifice() && (floorDef.passability == Traversability.Impassable || floorDef.Fillage == FillCategory.Full);
        if (isImpassable && attachmentType != RoofAttachmentType.Rooftop)
        {
          return true;
        }
      }

      return false;
    }

    return null;
  }

  public static bool? CheckCanPlaceBlueprintOver(BuildableDef newDef, ThingDef oldDef)
  {
    if (newDef == null || oldDef == null) return null;

    bool isNewRoofBuilding = newDef.HasModExtension<RoofBuilding>();
    bool isOldRoofBuilding = oldDef.HasModExtension<RoofBuilding>();

    if (isNewRoofBuilding && isOldRoofBuilding)
    {
      var typeNew = newDef.GetModExtension<RoofBuilding>().attachmentType;
      var typeOld = oldDef.GetModExtension<RoofBuilding>().attachmentType;
      if (typeNew == typeOld)
      {
        return false;
      }
      return true;
    }
    else if (isNewRoofBuilding || isOldRoofBuilding)
    {
      var roofDef = isNewRoofBuilding ? newDef : oldDef;
      var attachmentType = roofDef.GetModExtension<RoofBuilding>().attachmentType;
      var floorDef = isNewRoofBuilding ? oldDef : (newDef as ThingDef);

      if (floorDef != null)
      {
        bool isImpassable = floorDef.IsEdifice() && (floorDef.passability == Traversability.Impassable || floorDef.Fillage == FillCategory.Full);
        if (!isImpassable || attachmentType == RoofAttachmentType.Rooftop)
        {
          return false;
        }
        else
        {
          return true;
        }
      }
    }

    return null;
  }

  public static bool? CheckRoofBuildingSelectable(Thing thing)
  {
    if (thing != null && IsRoofBuildingOrBlueprintOrFrame(thing))
    {
      if (!ShouldRenderRoofBuilding(thing))
      {
        return false;
      }
    }
    return null;
  }

  public static bool? CheckRoofBuildingRender(Thing thing, Map map)
  {
    if (thing != null && IsRoofBuildingOrBlueprintOrFrame(thing))
    {
      var attachmentType = GetAttachmentType(thing);
      if (attachmentType == RoofAttachmentType.Rooftop)
      {
        return Find.PlaySettings != null && (Find.PlaySettings.showRoofOverlay || IsPlacingOrDesignatingRoofs(map));
      }
      else
      {
        return showRoofBuildings || IsPlacingOrDesignatingRoofs(map);
      }
    }
    return null;
  }

  public static bool DropDebris(Thing thing, Map targetMap)
  {
    int debrisCount = Rand.RangeInclusive(1, 2);
    for (int i = 0; i < debrisCount; i++)
    {
      var slag = ThingMaker.MakeThing(RimWorld.ThingDefOf.ChunkSlagSteel);
      GenPlace.TryPlaceThing(slag, thing.Position, targetMap, ThingPlaceMode.Near);
    }
    return true;
  }

  public static Vector3? GetRoofBuildingTrueCenter(Thing thing, Vector3 originalTrueCenter)
  {
    if (IsRoofBuildingOrBlueprintOrFrame(thing))
    {
      var attachmentType = GetAttachmentType(thing);
      if (attachmentType == RoofAttachmentType.Rooftop)
      {
        float roofAltitude = AltitudeLayer.MapDataOverlay.AltitudeFor() + 0.1f;
        var modified = originalTrueCenter;
        modified.y = roofAltitude;
        return modified;
      }
      else if (attachmentType == RoofAttachmentType.Hanging)
      {
        float hangingAltitude = AltitudeLayer.MapDataOverlay.AltitudeFor() - 0.1f;
        var modified = originalTrueCenter;
        modified.y = hangingAltitude;
        return modified;
      }
    }
    return null;
  }

  public static Vector3? GetRoofBuildingDrawPos(Thing thing, Vector3 originalDrawPos)
  {
    if (IsRoofBuildingOrBlueprintOrFrame(thing))
    {
      var attachmentType = GetAttachmentType(thing);
      if (attachmentType == RoofAttachmentType.Rooftop)
      {
        float roofAltitude = AltitudeLayer.MapDataOverlay.AltitudeFor() + 0.1f;
        var modified = originalDrawPos;
        modified.y = roofAltitude;
        return modified;
      }
      else if (attachmentType == RoofAttachmentType.Hanging)
      {
        float hangingAltitude = AltitudeLayer.MapDataOverlay.AltitudeFor() - 0.1f;
        var modified = originalDrawPos;
        modified.y = hangingAltitude;
        return modified;
      }
    }
    return null;
  }

  public static void DoMapControls(WidgetRow row)
  {
    row.ToggleableIcon(ref showRoofBuildings, ShowRoofBuildingsIcon, "ShowRoofBuildingsToggleButton".Translate(), RimWorld.SoundDefOf.Mouseover_ButtonToggle);
  }
}
