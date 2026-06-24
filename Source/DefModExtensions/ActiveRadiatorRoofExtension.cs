using Verse;

namespace SolarWeb.Stratum.DefModExtensions;

public class ActiveRadiatorRoofExtension : DefModExtension
{
  public float powerDrawActive = 20f;
  public float powerDrawStandby = 1f;
  public float coolingCapacity = 5f;
  public float targetTemperature = 21f;
}
