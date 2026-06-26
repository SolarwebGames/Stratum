using Verse;
using Verse.Sound;
using RimWorld;

namespace SolarWeb.Stratum.Things;

public class FireSprinkler : Building
{
  private int tickCounter = 0;

  protected override void Tick()
  {
    base.Tick();

    var power = GetComp<CompPowerTrader>();
    if (power != null && !power.PowerOn) return;

    tickCounter++;
    if (tickCounter >= 30)
    {
      tickCounter = 0;
      ExtinguishFires();
    }
  }

  private void ExtinguishFires()
  {
    var map = Map;
    if (map == null) return;

    try
    {
      int radius = 5;
      var center = Position;
      bool sprayed = false;

      foreach (var cell in GenRadial.RadialCellsAround(center, radius, true))
      {
        if (!cell.InBounds(map)) continue;
        var thingList = cell.GetThingList(map);
        if (thingList == null) continue;
        for (int i = thingList.Count - 1; i >= 0; i--)
        {
          var t = thingList[i];
          if (t is Fire fire)
          {
            fire.TakeDamage(new DamageInfo(DamageDefOf.Extinguish, 100));
            FleckMaker.ThrowSmoke(cell.ToVector3Shifted(), map, 0.8f);
            sprayed = true;
          }
        }
      }

      if (sprayed)
      {
        var sound = SoundDef.Named("Explosion_FirefoamPopper");
        sound?.PlayOneShot(new TargetInfo(Position, map));

        var waterFilthDef = DefOf.ThingDefOf.Filth_Water;
        if (waterFilthDef != null)
        {
          foreach (var cell in GenRadial.RadialCellsAround(center, radius, true))
          {
            if (cell.InBounds(map))
            {
              FilthMaker.TryMakeFilth(cell, map, waterFilthDef);
            }
          }
        }
      }
    }
    catch (System.Exception ex)
    {
      StratumLog.Error($"Error in FireSprinkler.ExtinguishFires: {ex}");
    }
  }

  public override void DrawExtraSelectionOverlays()
  {
    base.DrawExtraSelectionOverlays();
    GenDraw.DrawRadiusRing(Position, 5f);
  }
}
