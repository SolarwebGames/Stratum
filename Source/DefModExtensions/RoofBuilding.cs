using Verse;

namespace SolarWeb.Stratum.DefModExtensions;

public enum RoofAttachmentType
{
  Hanging,
  Rooftop
}

public class RoofBuilding : DefModExtension
{
  public RoofAttachmentType attachmentType = RoofAttachmentType.Hanging;
  public bool minifyOnRoofLoss = true;
  public bool destroyOnRoofLoss = false;
}
