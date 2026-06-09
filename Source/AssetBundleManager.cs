using System.Linq;
using UnityEngine;
using Verse;

namespace SolarWeb.Stratum;

[StaticConstructorOnStartup]
public static class AssetBundleManager
{
  public static AssetBundle? AssetBundle { get; private set; }

  static AssetBundleManager()
  {
    var mod = LoadedModManager.RunningModsListForReading.FirstOrDefault(m => m.PackageId.ToLower() == "solarweb.stratum");
    if (mod == null)
    {
      StratumLog.Error("Could not find mod with PackageId 'solarweb.stratum'");
      return;
    }

    if (mod.assetBundles.loadedAssetBundles.Count == 0)
    {
      StratumLog.Error("No asset bundles found.");
      return;
    }

    AssetBundle = mod.assetBundles.loadedAssetBundles[0];
    StratumLog.Debug($"Loaded asset bundle: {AssetBundle.name}");
  }
}