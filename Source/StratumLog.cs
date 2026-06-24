using System.Diagnostics;
using Verse;

namespace SolarWeb.Stratum
{
  public static class StratumLog
  {
    [Conditional("DEBUG")]
    public static void Debug(string message)
    {
      Log.Message($"[Stratum] {message}");
    }

    public static void Error(string error, bool once = false)
    {
      if (once)
      {
        Log.ErrorOnce($"[Stratum] {error}", error.GetHashCode());
      }
      else
      {
        Log.Error($"[Stratum] {error}");
      }
    }

    public static void Message(string message)
    {
      Log.Message($"[Stratum] {message}");
    }

    public static void Warning(string warning)
    {
      Log.Warning($"[Stratum] {warning}");
    }
  }
}
