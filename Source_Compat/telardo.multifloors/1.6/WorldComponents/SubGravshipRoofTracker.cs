using System.Collections.Generic;
using RimWorld.Planet;
using Verse;

using SolarWeb.Stratum.MapComponents;
using SolarWeb.Stratum.WorldComponents;

namespace SolarWeb.Stratum.MultiFloors.WorldComponents;

/// <summary>
/// Carries Stratum's per-cell roof metadata (stuff, glass tint, hit points) across a MultiFloors
/// sub-gravship flight.
/// </summary>
/// <remarks>
/// Stratum's own <see cref="GravshipRoofTracker"/> is keyed to the vanilla <c>Gravship</c> and only
/// covers the ground level. MultiFloors lifts every level above ground as a separate
/// <c>SubGravship</c>, whose saved roof data is just <c>IntVec3 -> RoofDef</c>, so without this the
/// upper decks land with default integrity values and lose their stuff and tint.
///
/// Entries are keyed by <c>SubGravship.GetUniqueLoadID()</c> and are persisted, because a ship can
/// sit in flight across a save. The restore pass unregisters each entry once it has been applied.
/// </remarks>
public class SubGravshipRoofTracker(World world) : WorldComponent(world)
{
  private Dictionary<string, GravshipRoofTracker.RoofData> subGravshipRoofs = [];

  public override void ExposeData()
  {
    base.ExposeData();
    Scribe_Collections.Look(ref subGravshipRoofs, "stratumSubGravshipRoofs", LookMode.Value, LookMode.Deep);

    if (Scribe.mode == LoadSaveMode.PostLoadInit)
    {
      subGravshipRoofs ??= [];
    }
  }

  public void Register(
    string subGravshipId,
    Dictionary<IntVec3, GravshipRoofTracker.RoofCellData> roofs,
    Dictionary<IntVec3, RoofConstructionTracker.ConstructionRecord> construction)
  {
    if (string.IsNullOrEmpty(subGravshipId)) return;
    subGravshipRoofs[subGravshipId] = new GravshipRoofTracker.RoofData { roofs = roofs, construction = construction };
  }

  public void Unregister(string subGravshipId)
  {
    if (string.IsNullOrEmpty(subGravshipId)) return;
    subGravshipRoofs.Remove(subGravshipId);
  }

  public bool TryGetRoofData(string subGravshipId, out GravshipRoofTracker.RoofData? data)
  {
    data = null;
    return !string.IsNullOrEmpty(subGravshipId) && subGravshipRoofs.TryGetValue(subGravshipId, out data);
  }
}
