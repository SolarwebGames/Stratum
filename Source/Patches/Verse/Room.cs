using HarmonyLib;
using Verse;

namespace SolarWeb.Stratum.Patches.Verse;

[HarmonyPatch]
public static class Room_Patch
{
  [HarmonyPatch(typeof(Room), nameof(Room.Notify_RoofChanged))]
  [HarmonyPostfix]
  public static void Notify_RoofChanged_Postfix(Room __instance)
  {
    AccessTools.Field(typeof(Room), "statsAndRoleDirty").SetValue(__instance, true);
  }
}
