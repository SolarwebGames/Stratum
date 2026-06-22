using System.Collections.Generic;
using System.Text;
using RimWorld;
using UnityEngine;
using Verse;

using SolarWeb.Stratum.DefModExtensions;
using SolarWeb.Stratum.Graphics;
using SolarWeb.Stratum.MapComponents;
using SolarWeb.Stratum.Stats;

namespace SolarWeb.Stratum.Things;

[StaticConstructorOnStartup]
public class RoofFrame : Building, IThingHolder, IConstructible, IHaulEnroute, ICancelableByDesignator
{
  public ThingOwner<Thing> resourceContainer = null!;
  public RoofDef targetRoofDef = null!;
  public ThingDef? targetRoofStuff = null;
  public UnityEngine.Color? glassTint;

  private Material? cachedCornerMat;
  private Material? cachedTileMat;

  private static readonly Material UnderfieldMat =
    MaterialPool.MatFrom(RimWorldTextures.Things.Building.BuildingFrame.Underfield, ShaderDatabase.Transparent);
  private static readonly Texture2D CornerTex =
    ContentFinder<Texture2D>.Get(RimWorldTextures.Things.Building.BuildingFrame.Corner);
  private static readonly Texture2D ProgressTex =
    ContentFinder<Texture2D>.Get(RimWorldTextures.UI.Designators.BuildRoofArea);

  private Material CornerMat => cachedCornerMat ??=
    MaterialPool.MatFrom(CornerTex, ShaderDatabase.MetaOverlay, DrawColor);
  private Material ProgressMat => cachedTileMat ??=
    MaterialPool.MatFrom(ProgressTex, ShaderDatabase.Transparent, Color.white);

  private static readonly MaterialPropertyBlock PropertyBlock = new();
  private static readonly int MainTexSTID = Shader.PropertyToID("_MainTex_ST");
  private static readonly int ColorID = Shader.PropertyToID("_Color");
  private static readonly Color BlueprintColor = new(0.13f, 0.35f, 0.44f, 0.45f);

  private float MaterialProgress
  {
    get
    {
      var costList = TotalMaterialCost();
      if (costList.Count == 0) return 1f;
      int totalNeeded = 0, totalHave = 0;
      foreach (var cost in costList)
      {
        totalNeeded += cost.count;
        totalHave += cost.count - ThingCountNeeded(cost.thingDef);
      }
      return totalNeeded > 0 ? Mathf.Clamp01((float)totalHave / totalNeeded) : 1f;
    }
  }

  private float WorkProgress
  {
    get
    {
      if (Spawned)
      {
        var tracker = Map.GetComponent<RoofConstructionTracker>();
        if (tracker != null && tracker.TryGetRecord(Position, out var rec))
        {
          return Mathf.Clamp01(rec.workDone / rec.workTotal);
        }
      }
      return 0f;
    }
  }

  private BuildableRoofExtension? CostExt => targetRoofDef?.GetModExtension<BuildableRoofExtension>();

  public RoofFrame()
  {
    resourceContainer = new ThingOwner<Thing>(this, oneStackOnly: false);
  }

  public override string Label
  {
    get
    {
      string text = targetRoofDef?.label ?? "Stratum_Roof".Translate();
      if (targetRoofStuff != null)
        return "Stratum_RoofFrameLabel".Translate("ThingMadeOfStuffLabel".Translate(targetRoofStuff.LabelAsStuff, text));
      return "Stratum_RoofFrameLabel".Translate(text);
    }
  }

  public override Color DrawColor
  {
    get
    {
      if (glassTint.HasValue) return glassTint.Value;
      return RoofStatCache.GetColor(targetRoofDef, targetRoofStuff);
    }
  }

  public bool CanCancel => Faction == Faction.OfPlayer && !Destroyed;

  public void CancelByDesignator()
  {
    Destroy(DestroyMode.Cancel);
  }

  public ThingOwner GetDirectlyHeldThings() => resourceContainer;

  public void GetChildHolders(List<IThingHolder> outChildren)
  {
    ThingOwnerUtility.AppendThingHoldersFromThings(outChildren, GetDirectlyHeldThings());
  }

  public ThingDef? EntityToBuildStuff() => targetRoofStuff;

  public List<ThingDefCountClass> TotalMaterialCost()
    => CostExt?.buildableDef?.CostListAdjusted(targetRoofStuff) ?? [];

  public int ThingCountNeeded(ThingDef stuff)
  {
    foreach (var cost in TotalMaterialCost())
      if (cost.thingDef == stuff)
        return Mathf.Max(0, cost.count - resourceContainer.TotalStackCountOfDef(stuff));
    return 0;
  }

  public bool IsCompleted() => HasAllMaterials();

  public int SpaceRemainingFor(ThingDef stuff) => ThingCountNeeded(stuff);

  public int GetSpaceRemainingWithEnroute(ThingDef stuff, Pawn haulingPawn)
  {
    int space = SpaceRemainingFor(stuff);
    foreach (var pawn in Map.mapPawns.AllPawnsSpawned)
    {
      if (pawn == haulingPawn || pawn.jobs?.curJob == null) continue;
      var job = pawn.jobs.curJob;

      if (job.def == DefOf.JobDefOf.DeliverRoofIngredients)
      {
        bool isTarget = job.targetB.Thing == this;
        if (!isTarget && job.targetQueueB != null)
        {
          foreach (var t in job.targetQueueB)
          {
            if (t.Thing == this) { isTarget = true; break; }
          }
        }

        if (isTarget && (pawn.carryTracker?.CarriedThing?.def == stuff || job.targetA.Thing?.def == stuff))
        {
          space -= job.count;
        }
      }
      else if (job.def == JobDefOf.HaulToContainer && job.targetB.Thing == this && (pawn.carryTracker?.CarriedThing?.def == stuff || job.targetA.Thing?.def == stuff))
      {
        space -= job.count;
      }
    }
    return Mathf.Max(0, space);
  }

  public bool HasAllMaterials()
  {
    foreach (var cost in TotalMaterialCost())
      if (ThingCountNeeded(cost.thingDef) > 0) return false;
    return true;
  }

  public override IEnumerable<Gizmo> GetGizmos()
  {
    yield return new Command_Action
    {
      defaultLabel = "CommandCancelConstructionLabel".Translate(),
      defaultDesc = "CommandCancelConstructionDesc".Translate(),
      icon = ContentFinder<Texture2D>.Get("UI/Designators/Cancel"),
      hotKey = KeyBindingDefOf.Designator_Cancel,
      action = delegate
      {
        Map.GetComponent<RoofConstructionTracker>().RemoveRecord(Position, DestroyMode.Refund);
      }
    };
  }

  public override void ExposeData()
  {
    base.ExposeData();
    Scribe_Defs.Look(ref targetRoofDef, "targetRoofDef");
    Scribe_Defs.Look(ref targetRoofStuff, "targetRoofStuff");
    Scribe_Values.Look(ref glassTint, "glassTint");
    Scribe_Deep.Look(ref resourceContainer, "resourceContainer", this);
  }

  public override void Destroy(DestroyMode mode = DestroyMode.Vanish)
  {
    if (Destroyed) return;

    if (mode != DestroyMode.Vanish && mode != DestroyMode.KillFinalize && Spawned)
    {
      var map = Map;
      var pos = Position;
      var items = new List<Thing>(resourceContainer.InnerListForReading);
      foreach (var t in items)
      {
        resourceContainer.Remove(t);
        GenPlace.TryPlaceThing(t, pos, map, ThingPlaceMode.Near);
      }
    }

    var oldMap = Map;
    var oldPos = Position;
    var oldSpawned = Spawned;

    resourceContainer.ClearAndDestroyContents();
    base.Destroy(mode);

    if (oldSpawned)
    {
      oldMap.GetComponent<RoofConstructionTracker>()?.RemoveRecordInternal(oldPos);
    }
  }

  protected override void DrawAt(Vector3 drawLoc, bool flip = false)
  {
    var s = new Vector3(def.size.x * 1.15f, 1f, def.size.z * 1.15f);
    var m = default(Matrix4x4);
    m.SetTRS(drawLoc, Rotation.AsQuat, s);

    PropertyBlock.Clear();
    PropertyBlock.SetColor(ColorID, DrawColor.ToTransparent(0.5f));
    UnityEngine.Graphics.DrawMesh(MeshPool.plane10, m, UnderfieldMat, 0, null, 0, PropertyBlock);

    float cornerSize = Mathf.Min(RotatedSize.x, RotatedSize.z) * 0.38f;
    var corners = new[] {
        new IntVec3(-1, 0, -1), new IntVec3(-1, 0, 1),
        new IntVec3( 1, 0,  1), new IntVec3( 1, 0, -1)
      };
    for (int i = 0; i < 4; i++)
    {
      var offset = new Vector3(
        corners[i].x * (RotatedSize.x / 2f - cornerSize / 2f),
        0f,
        corners[i].z * (RotatedSize.z / 2f - cornerSize / 2f));
      var cm = default(Matrix4x4);
      cm.SetTRS(drawLoc + Vector3.up * 0.03f + offset,
                new Rot4(i).AsQuat,
                new Vector3(cornerSize, 1f, cornerSize));
      UnityEngine.Graphics.DrawMesh(MeshPool.plane10, cm, CornerMat, 0);
    }

    float matProgress = MaterialProgress;
    float workProgress = WorkProgress;

    if (matProgress > 0.01f)
    {
      float displayProgress = (matProgress < 1f) ? matProgress : 1f;
      DrawFill(drawLoc + Vector3.up * 0.01f, displayProgress, BlueprintColor, scroll: false);
    }

    if (workProgress > 0.01f)
    {
      DrawFill(drawLoc + Vector3.up * 0.02f, workProgress, DrawColor, scroll: true);
    }

    Comps_PostDraw();
  }

  private void DrawFill(Vector3 drawLoc, float progress, Color color, bool scroll)
  {
    var fillSize = new Vector3(RotatedSize.x * 0.9f, 1f, RotatedSize.z * 0.9f * progress);
    var fillLoc = drawLoc;

    float localOffsetZ = -(1f - progress) * (RotatedSize.z * 0.9f) * 0.5f;
    fillLoc += Rotation.AsQuat * new Vector3(0f, 0f, localOffsetZ);

    var fm = default(Matrix4x4);
    fm.SetTRS(fillLoc, Rotation.AsQuat, fillSize);

    PropertyBlock.Clear();
    PropertyBlock.SetColor(ColorID, color);

    if (scroll)
    {
      PropertyBlock.SetVector(MainTexSTID, new Vector4(1f, progress, 0f, 1f - progress));
    }
    else
    {
      PropertyBlock.SetVector(MainTexSTID, new Vector4(1f, progress, 0f, 0f));
    }

    UnityEngine.Graphics.DrawMesh(MeshPool.plane10, fm, ProgressMat, 0, null, 0, PropertyBlock);
  }

  public override string GetInspectString()
  {
    var sb = new StringBuilder();

    if (Spawned)
    {
      var tracker = Map.GetComponent<RoofConstructionTracker>();
      if (tracker != null && tracker.TryGetRecord(Position, out var rec))
      {
        if (rec.workDone > 0)
        {
          sb.AppendLine("Stratum_ConstructionProgress".Translate((rec.workDone / rec.workTotal).ToStringPercent()));
        }
      }
    }

    var costList = TotalMaterialCost();
    if (costList.Count > 0)
    {
      sb.AppendLine("ContainedResources".Translate() + ":");
      foreach (var cost in costList)
      {
        int have = cost.count - ThingCountNeeded(cost.thingDef);
        sb.AppendLine($"  {cost.thingDef.LabelCap}: {have} / {cost.count}");
      }
    }
    return sb.ToString().TrimEndNewlines();
  }
}
