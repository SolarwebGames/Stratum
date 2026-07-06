using UnityEngine;
using Verse;
using RimWorld;

namespace SolarWeb.Stratum.AI.Designators;

public class SmoothRoof : Designator_Smooth
{
  public SmoothRoof()
  {
    defaultLabel = "SolarWeb_Stratum_DesignatorSmoothRoof".Translate();
    defaultDesc = "SolarWeb_Stratum_DesignatorSmoothRoofDesc".Translate();
    icon = ContentFinder<Texture2D>.Get("UI/Designators/SmoothSurface");
  }

  public override void DesignateSingleCell(IntVec3 c)
  {
    if (DebugSettings.godMode)
    {
      var roof = Map.roofGrid.RoofAt(c);
      var smoothedRoof = GetSmoothedVersion(roof);
      if (smoothedRoof != roof)
      {
        Map.roofGrid.SetRoof(c, smoothedRoof);
        FleckMaker.ThrowMetaPuffs(new TargetInfo(c, Map));
      }
    }
    else
    {
      Map.designationManager.AddDesignation(new Designation(c, DefOf.DesignationDefOf.SmoothRoof));
    }
  }

  public override AcceptanceReport CanDesignateCell(IntVec3 c)
  {
    AcceptanceReport result = base.CanDesignateCell(c);
    if (!result.Accepted) return result;

    var roof = Map.roofGrid.RoofAt(c);
    if (roof == null || (roof != RoofDefOf.RoofRockThin && roof != RoofDefOf.RoofRockThick))
    {
      return "SolarWeb_Stratum_MessageMustDesignateNaturalRoof".Translate();
    }

    if (Map.designationManager.DesignationAt(c, DefOf.DesignationDefOf.SmoothRoof) != null)
    {
      return "SurfaceBeingSmoothed".Translate();
    }

    return AcceptanceReport.WasAccepted;
  }

  private static RoofDef GetSmoothedVersion(RoofDef original)
  {
    if (original == RoofDefOf.RoofRockThin)
      return DefOf.RoofDefOf.RoofThinRockSmoothed;
    if (original == RoofDefOf.RoofRockThick)
      return DefOf.RoofDefOf.RoofOverheadMountainSmoothed;
    return original;
  }
}
