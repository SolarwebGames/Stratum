using RimWorld;
using UnityEngine;
using Verse;

namespace SolarWeb.Stratum.ThingComps;

public class RooftopWindTurbine : CompPowerPlant
{
  protected override float DesiredPowerOutput
  {
    get
    {
      if (parent == null || parent.Map == null) return 0f;
      float windSpeed = parent.Map.windManager.WindSpeed;
      float baseNominalPower = 600f;
      return Mathf.Min(baseNominalPower * windSpeed, 1200f);
    }
  }

  private float lastWindSpeed = -1f;
  private string cachedWindSpeedText = "";
  private string cachedWindEffText = "";

  public override string CompInspectStringExtra()
  {
    if (parent == null || parent.Map == null) return base.CompInspectStringExtra();

    string text = base.CompInspectStringExtra();
    float windSpeed = parent.Map.windManager.WindSpeed;

    if (Mathf.Abs(windSpeed - lastWindSpeed) > 0.01f || string.IsNullOrEmpty(cachedWindSpeedText))
    {
      lastWindSpeed = windSpeed;
      cachedWindSpeedText = "SolarWeb_Stratum_WindSpeed".Translate(windSpeed.ToString("F2"));
      cachedWindEffText = "SolarWeb_Stratum_WindEfficiency".Translate(Mathf.Min(windSpeed * 100f, 200f).ToString("F0"));
    }

    return text + "\n" + cachedWindSpeedText + "\n" + cachedWindEffText;
  }
}
