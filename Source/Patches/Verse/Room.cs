using System.Reflection;
using HarmonyLib;
using Verse;

namespace SolarWeb.Stratum.Patches;

[HarmonyPatch(typeof(Room))]
public static class Room_Patch
{
  private static FieldInfo statsAndRoleDirty = AccessTools.Field(typeof(Room), "statsAndRoleDirty");

  [HarmonyPatch(nameof(Room.Notify_RoofChanged))]
  [HarmonyPostfix]
  public static void Notify_RoofChanged_Postfix(Room __instance)
  {
    statsAndRoleDirty.SetValue(__instance, true);
  }
}
