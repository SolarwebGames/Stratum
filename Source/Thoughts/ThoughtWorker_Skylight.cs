using RimWorld;
using Verse;

namespace SolarWeb.Stratum.Thoughts;

public class ThoughtWorker_Skylight : ThoughtWorker
{
  protected override ThoughtState CurrentStateInternal(Pawn p)
  {
    if (!p.Spawned || !p.Awake())
    {
      return ThoughtState.Inactive;
    }

    Room room = p.GetRoom();
    if (room == null || room.PsychologicallyOutdoors)
    {
      return ThoughtState.Inactive;
    }

    float skylightPct = room.GetStat(DefOf.RoomStatDefOf.SkylightPercentage);
    if (skylightPct <= 0)
    {
      return ThoughtState.Inactive;
    }

    int scoreStageIndex = DefOf.RoomStatDefOf.SkylightPercentage.GetScoreStageIndex(skylightPct);

    var isIndoorsPawn = p.story?.traits?.HasTrait(TraitDefOf.Undergrounder) == true ||
                        (p.Ideo != null && p.Ideo.HasMeme(MemeDefOf.Tunneler));

    if (isIndoorsPawn && scoreStageIndex > 0)
    {
      scoreStageIndex += 3;
    }

    if (scoreStageIndex >= def.stages.Count || def.stages[scoreStageIndex] == null)
    {
      return ThoughtState.Inactive;
    }

    return ThoughtState.ActiveAtStage(scoreStageIndex);
  }
}
