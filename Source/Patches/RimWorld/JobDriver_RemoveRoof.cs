using System.Reflection;
using HarmonyLib;
using RimWorld;
using SolarWeb.Stratum.DefModExtensions;
using Verse;

namespace SolarWeb.Stratum.Patches;

[HarmonyPatch]
public static class JobDriver_RemoveRoof_Patch
{
  public static PropertyInfo CellProperty = AccessTools.Property(typeof(JobDriver_RemoveRoof), "Cell");

  [HarmonyPatch(typeof(JobDriver_RemoveRoof), "DoEffect")]
  [HarmonyPrefix]
  public static bool DoEffect_Prefix(JobDriver_RemoveRoof __instance)
  {
    var map = __instance.pawn.Map;
    var cell = (IntVec3)CellProperty.GetValue(__instance);

    var roof = map.roofGrid.RoofAt(cell);
    if (roof == null) return false;

    var extension = roof.GetModExtension<BuildableRoofExtension>();
    var bDef = extension?.buildableDef;

    var integrityGrid = map.GetComponent<MapComponents.RoofIntegrityGrid>();
    var stuff = integrityGrid?.GetStuff(cell);
    integrityGrid?.RemoveRoof(cell);
    map.areaManager.NoRoof[cell] = false;
    map.areaManager.NoRoof.MarkForDraw();
    map.areaManager.BuildRoof[cell] = false;
    map.areaManager.BuildRoof.MarkForDraw();

    if (bDef != null)
    {
      var costList = bDef.CostListAdjusted(stuff);
      if (!costList.NullOrEmpty())
      {
        float fraction = bDef.resourcesFractionWhenDeconstructed;
        foreach (var cost in costList)
        {
          int count = GenMath.RoundRandom(cost.count * fraction);
          if (count <= 0) continue;
          var thing = ThingMaker.MakeThing(cost.thingDef);
          thing.stackCount = count;
          GenPlace.TryPlaceThing(thing, cell, map, ThingPlaceMode.Near);
        }
      }
    }

    map.roofGrid.SetRoof(cell, null);
    map.mapDrawer.MapMeshDirty(cell, MapMeshFlagDefOf.Roofs);
    return false;
  }
}
