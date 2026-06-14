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

      var cell = Verse.UI.MouseCell();
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

      return label;
    }
  }
}
