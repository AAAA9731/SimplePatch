using System;
using System.Collections.Generic;
using HarmonyLib;

namespace SimplePatch
{
	// A self-contained patch. To add one: implement IPatch (see Patches/) and register it in PatchHost.Patches.
	public interface IPatch
	{
		string Name { get; }
		string Description { get; }
		// Apply the patch's Harmony hooks (use h.CreateClassProcessor(typeof(X)).Patch() for each [HarmonyPatch] class).
		// Return false if the patch could not be applied.
		bool Install(Harmony h);
	}

	// Single entry point shared by the loader-free injector (SimplePatchBoot) and the BepInEx plugin (SimplePatchMod).
	public static class PatchHost
	{
		public const string HarmonyIdPrefix = "local.aic.simplepatch.";

		public static Action<string> LogInfo = delegate { };
		public static Action<string> LogWarn = delegate { };
		public static Action<string> LogError = delegate { };

		// Front-ends set this to decide per patch (by IPatch.Name) whether it should be installed.
		public static Func<string, bool> IsEnabled = name => true;

		public static readonly IPatch[] Patches = new IPatch[]
		{
			new TxLoadPatch(),
			new DebugCursorPatch(),
			new PicLoadPatch(),
		};

		public static void InstallAll()
		{
			int ok = 0;
			foreach (IPatch p in Patches)
			{
				bool enabled;
				try
				{
					enabled = IsEnabled(p.Name);
				}
				catch (Exception ex)
				{
					LogError("[" + p.Name + "] enable check failed: " + ex);
					enabled = true;
				}
				if (!enabled)
				{
					LogInfo("[" + p.Name + "] disabled, skipped.");
					continue;
				}
				try
				{
					if (p.Install(new Harmony(HarmonyIdPrefix + p.Name)))
					{
						ok++;
						LogInfo("[" + p.Name + "] installed: " + p.Description);
					}
					else
					{
						LogError("[" + p.Name + "] failed to install.");
					}
				}
				catch (Exception ex)
				{
					LogError("[" + p.Name + "] install threw: " + ex);
				}
			}
			LogInfo(ok.ToString() + "/" + Patches.Length.ToString() + " patch(es) installed.");
		}

		public static IEnumerable<string> PatchNames()
		{
			foreach (IPatch p in Patches)
			{
				yield return p.Name;
			}
		}
	}
}
