using UnityEngine;
using Verse;
using RimWorld;

namespace SolarWeb.Stratum.Utilities;

public static class RoofFleckMaker
{
  public static void ThrowSmoke(Vector3 loc, Map map, float size, FleckDef fleckDef)
  {
    if (loc.ShouldSpawnMotesAt(map))
    {
      FleckCreationData data = FleckMaker.GetDataStatic(loc, map, fleckDef, Rand.Range(1.5f, 2.5f) * size);
      data.rotationRate = Rand.Range(-30f, 30f);
      data.velocityAngle = Rand.Range(30, 40);
      data.velocitySpeed = Rand.Range(0.5f, 0.7f);
      map.flecks.CreateFleck(data);
    }
  }

  public static void ThrowMicroSparks(Vector3 loc, Map map, FleckDef fleckDef)
  {
    if (loc.ShouldSpawnMotesAt(map))
    {
      loc -= new Vector3(0.5f, 0f, 0.5f);
      loc += new Vector3(Rand.Value, 0f, Rand.Value);
      FleckCreationData data = FleckMaker.GetDataStatic(loc, map, fleckDef, Rand.Range(0.8f, 1.2f));
      data.rotationRate = Rand.Range(-12f, 12f);
      data.velocityAngle = Rand.Range(35, 45);
      data.velocitySpeed = 1.2f;
      map.flecks.CreateFleck(data);
    }
  }
}
