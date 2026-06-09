using System.Collections.Generic;
using SolarWeb.Stratum.AI.Designators;
using SolarWeb.Stratum.DefModExtensions;
using Verse;

namespace SolarWeb.Stratum;

[StaticConstructorOnStartup]
public static class BuildableRoofGenerator
{
  public static Dictionary<RoofDef, BuildCustomRoof> RoofToDesignator = [];

  static BuildableRoofGenerator()
  {
    GenerateDesignators();
  }

  private static void GenerateDesignators()
  {
    foreach (var roofDef in DefDatabase<RoofDef>.AllDefs)
    {
      var extension = roofDef.GetModExtension<BuildableRoofExtension>();
      if (extension == null) continue;

      var designator = new BuildCustomRoof(roofDef, extension);

      if (extension.designationCategory != null)
        extension.designationCategory.AllResolvedDesignators.Add(designator);

      RoofToDesignator[roofDef] = designator;
    }
  }
}
