using System.Collections;
using HarmonyLib;
using RimWorld.Planet;
using Verse;

using SolarWeb.Stratum.Hooks;
using SolarWeb.Stratum.Utilities;

namespace SolarWeb.Stratum.Patches.RimWorld;

[HarmonyPatch(typeof(OrbitalScannerWorldComponent))]
public static class OrbitalScannerWorldComponent_Patch
{
  [HarmonyPatch(nameof(OrbitalScannerWorldComponent.WorldComponentTick))]
  [HarmonyPrefix]
  public static bool WorldComponentTick_Prefix(object __instance, ref int ___lastFoundSignal, IList ___workingScanners)
  {
    bool onCooldown = ___lastFoundSignal > 0 && Find.TickManager.TicksGame < ___lastFoundSignal + 1080000;
    if (onCooldown || ___workingScanners == null || ___workingScanners.Count == 0)
    {
      return false;
    }

    float totalWeight = 0f;
    for (int i = 0; i < ___workingScanners.Count; i++)
    {
      var scanner = ___workingScanners[i] as ThingComp;
      if (scanner != null && scanner.parent != null)
      {
        float mult = ScannerBoosterUtility.GetScanSpeed(scanner.parent, 1f);
        totalWeight += mult;
      }
    }

    float baseMtb = 60000f;
    float adjustedMtb = baseMtb / totalWeight;

    if (Rand.MTBEventOccurs(adjustedMtb, 1f, 1f))
    {
      var randomScanner = ___workingScanners[Rand.Range(0, ___workingScanners.Count)];
      var receiveSignalMethod = AccessTools.Method(randomScanner.GetType(), "ReceiveSignal");
      receiveSignalMethod?.Invoke(randomScanner, null);

      ___lastFoundSignal = Find.TickManager.TicksGame;
    }
    ___workingScanners.Clear();

    return false;
  }
}
