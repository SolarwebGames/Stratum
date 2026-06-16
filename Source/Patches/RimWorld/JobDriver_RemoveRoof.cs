using System.Reflection;
using HarmonyLib;
using RimWorld;
using SolarWeb.Stratum.DefModExtensions;
using Verse;

namespace SolarWeb.Stratum.Patches;

[HarmonyPatch(typeof(JobDriver_RemoveRoof))]
public static class JobDriver_RemoveRoof_Patch
{
  public static PropertyInfo CellProperty = AccessTools.Property(typeof(JobDriver_RemoveRoof), "Cell");

  public struct RemovalState
  {
    public IntVec3 cell;
    public Map map;
    public RoofDef roof;
    public ThingDef stuff;
  }

  [HarmonyPatch("DoEffect")]
  [HarmonyPrefix]
  public static void DoEffect_Prefix(JobDriver_RemoveRoof __instance, out RemovalState __state)
  {
    var map = __instance.pawn.Map;
    var cell = (IntVec3)CellProperty.GetValue(__instance);
    var roof = map.roofGrid.RoofAt(cell);
    var stuff = map.GetComponent<MapComponents.RoofIntegrityGrid>()?.GetStuff(cell);

    __state = new RemovalState
    {
      cell = cell,
      map = map,
      roof = roof,
      stuff = stuff!
    };
  }

  [HarmonyPatch("DoEffect")]
  [HarmonyPostfix]
  public static void DoEffect_Postfix(RemovalState __state)
  {
    if (__state.roof == null) return;

    var extension = __state.roof.GetModExtension<BuildableRoofExtension>();
    var bDef = extension?.buildableDef;

    if (bDef != null)
    {
      var costList = bDef.CostListAdjusted(__state.stuff);
      if (!costList.NullOrEmpty())
      {
        float fraction = bDef.resourcesFractionWhenDeconstructed;
        foreach (var cost in costList)
        {
          int count = GenMath.RoundRandom(cost.count * fraction);
          if (count <= 0) continue;
          var thing = ThingMaker.MakeThing(cost.thingDef);
          thing.stackCount = count;
          GenPlace.TryPlaceThing(thing, __state.cell, __state.map, ThingPlaceMode.Near);
        }
      }
    }

    __state.map.GetComponent<MapComponents.RoofIntegrityGrid>()?.RemoveRoof(__state.cell);
    __state.map.mapDrawer.MapMeshDirty(__state.cell, MapMeshFlagDefOf.Roofs);
  }
}
