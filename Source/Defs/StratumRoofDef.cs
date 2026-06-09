using Verse;

using SolarWeb.Stratum.DefModExtensions;
using SolarWeb.Stratum.Stats;

namespace SolarWeb.Stratum.Defs;

public class StratumRoofDef : RoofDef
{
  private BuildableRoofExtension? extension;
  private bool extensionCached;

  public BuildableRoofExtension? Extension
  {
    get
    {
      if (!extensionCached)
      {
        extension = GetModExtension<BuildableRoofExtension>();
        extensionCached = true;
      }
      return extension;
    }
  }

  public override TaggedString LabelCap
  {
    get
    {
      var map = Find.CurrentMap;
      if (map == null)
        return base.LabelCap;

      var cell = UI.MouseCell();
      if (!cell.InBounds(map) || map.roofGrid.RoofAt(cell) != this)
        return base.LabelCap;

      var integrityGrid = map.GetComponent<MapComponents.RoofIntegrityGrid>();
      if (integrityGrid == null)
        return base.LabelCap;

      ThingDef? stuff = integrityGrid.GetStuff(cell);
      TaggedString label = (stuff != null)
        ? "ThingMadeOfStuffLabel".Translate(stuff.LabelAsStuff, base.label).CapitalizeFirst()
        : base.LabelCap;

      short hp = integrityGrid.GetHitPoints(cell);
      short maxHp = (short)RoofStatCache.GetMaxHitPoints(this, stuff);

      if (hp < maxHp && maxHp > 0)
      {
        float pct = (float)hp / maxHp;
        label += $" ({hp} / {maxHp} {pct.ToStringPercent("F0")})";
      }

      if (RoofStatCache.GetSolarEfficiency(this) > 0f)
      {
        var solarComp = map.GetComponent<MapComponents.SolarRoofMapComponent>();
        if (solarComp != null && solarComp.TryGetSolarNetworkPower(cell, out var cellStats, out var netStats))
        {
          label += " - " + "Stratum_SolarPowerStats".Translate(
            cellStats.currentPower.ToString("F0"),
            cellStats.maxPower.ToString("F0"),
            netStats.currentPower.ToString("F0"),
            netStats.maxPower.ToString("F0")
          );
        }
      }

      return label;
    }
  }
}
