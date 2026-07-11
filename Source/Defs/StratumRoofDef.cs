using System.Collections.Generic;
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

      var coating = map.GetComponent<MapComponents.SkylightCoating>();
      if (coating != null)
      {
        float dirt = coating.GetDirtLevel(cell);
        float pollen = coating.GetPollenLevel(cell);
        float snow = coating.GetSnowLevel(cell);
        List<string> details = [];

        if (dirt > 0.01f)
        {
          details.Add($"{"SolarWeb_Stratum_Dust".Translate()}: {dirt.ToStringPercent("F0")}");
        }

        if (pollen > 0.01f)
        {
          details.Add($"{"SolarWeb_Stratum_Pollen".Translate()}: {pollen.ToStringPercent("F0")}");
        }

        if (snow > 0.01f)
        {
          details.Add($"{"SolarWeb_Stratum_Snow".Translate()}: {snow.ToStringPercent("F0")}");
        }

        if (details.Count > 0)
        {
          label += $" ({string.Join(", ", details)})";
        }
      }

      return label;
    }
  }
}
