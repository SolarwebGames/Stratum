using System;
using UnityEngine;
using Verse;
using RimWorld;

using SolarWeb.Stratum.DefModExtensions;
using SolarWeb.Stratum.Hooks;

namespace SolarWeb.Stratum.Utilities;

public static class RoofBuildings
{
  public static bool showRoofBuildings = false;
  public static bool isDeconstructingRoof = false;

  private static Texture2D? showRoofBuildingsIcon;
  public static Texture2D ShowRoofBuildingsIcon => showRoofBuildingsIcon ??= CreateDefaultToggleIcon();

  private static Material? attachmentIndicatorMat;
  public static Material AttachmentIndicatorMat => attachmentIndicatorMat ??= new Material(ShaderDatabase.MetaOverlay)
  {
    mainTexture = CreateRoofAttachmentIndicatorTexture()
  };

  public static bool ShouldRenderRoofBuilding(Thing t)
  {
    if (t == null || t.def == null) return false;
    var map = t.Map;
    if (map != null)
    {
      var registry = MapHookRegistry.Get(map);
      if (registry != null)
      {
        var res = registry.CheckRoofBuildingRender(t);
        if (res.HasValue) return res.Value;
      }
    }

    var attachmentType = GetAttachmentType(t);
    if (attachmentType == RoofAttachmentType.Rooftop)
    {
      return Find.PlaySettings != null && (Find.PlaySettings.showRoofOverlay || IsPlacingOrDesignatingRoofs(t.Map));
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

  public static bool IsRoofBuildingOrBlueprintOrFrame(Thing t)
  {
    if (t == null || t.def == null) return false;
    if (t.def.HasModExtension<RoofBuilding>()) return true;
    if (t is Blueprint blueprint && blueprint.def != null && blueprint.def.entityDefToBuild is ThingDef buildDef && buildDef.HasModExtension<RoofBuilding>()) return true;
    if (t is Frame frame && frame.def != null && frame.def.entityDefToBuild is ThingDef frameDef && frameDef.HasModExtension<RoofBuilding>()) return true;
    return false;
  }

  public static RoofAttachmentType GetAttachmentType(Thing t)
  {
    if (t == null || t.def == null) return RoofAttachmentType.Hanging;
    var ext = t.def.GetModExtension<RoofBuilding>();
    if (ext != null) return ext.attachmentType;

    if (t is Blueprint blueprint && blueprint.def != null && blueprint.def.entityDefToBuild is ThingDef buildDef)
    {
      var buildExt = buildDef.GetModExtension<RoofBuilding>();
      if (buildExt != null) return buildExt.attachmentType;
    }
    else if (t is Frame frame && frame.def != null && frame.def.entityDefToBuild is ThingDef frameDef)
    {
      var frameExt = frameDef.GetModExtension<RoofBuilding>();
      if (frameExt != null) return frameExt.attachmentType;
    }
    return RoofAttachmentType.Hanging;
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

  private static Texture2D CreateRoofAttachmentIndicatorTexture()
  {
    Texture2D tex = new Texture2D(32, 32, TextureFormat.RGBA32, false);
    for (int x = 0; x < 32; x++)
    {
      for (int y = 0; y < 32; y++)
      {
        tex.SetPixel(x, y, Color.clear);
      }
    }
    for (int x = 0; x < 32; x++)
    {
      for (int y = 0; y < 32; y++)
      {
        double dist = Math.Sqrt((x - 16) * (x - 16) + (y - 16) * (y - 16));
        if (dist >= 5.0 && dist <= 7.0)
        {
          tex.SetPixel(x, y, new Color(1f, 0.6f, 0f, 0.8f));
        }
        else if (dist >= 1.0 && dist <= 2.0)
        {
          tex.SetPixel(x, y, new Color(1f, 0.6f, 0f, 0.8f));
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
      var floorDef = isConstructibleRoofBuilding ? defExisting : defConstructible;
      if (floorDef != null)
      {
        if (floorDef.IsEdifice() && (floorDef.passability == Traversability.Impassable || floorDef.Fillage == FillCategory.Full))
        {
          return true;
        }
      }

      return false;
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
        float roofAltitude = AltitudeLayer.MoteOverhead.AltitudeFor() + 0.1f;
        var modified = originalTrueCenter;
        modified.y = roofAltitude;
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
        float roofAltitude = AltitudeLayer.MoteOverhead.AltitudeFor() + 0.1f;
        var modified = originalDrawPos;
        modified.y = roofAltitude;
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
