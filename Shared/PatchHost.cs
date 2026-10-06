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

		// Sinks, set by the front-ends. Patches log through LogInfo/LogWarn/LogError, which drop a message
		// identical to the previous one (event commands run in both the cache-read and the run phase).
		public static Action<string> SinkInfo = delegate { };
		public static Action<string> SinkWarn = delegate { };
		public static Action<string> SinkError = delegate { };

		private static string lastMsg;

		private static bool Repeated(string kind, string s)
		{
			string key = kind + s;
			if (key == lastMsg)
			{
				return true;
			}
			lastMsg = key;
			return false;
		}

		public static void LogInfo(string s) { if (!Repeated("I", s)) SinkInfo(s); }
		public static void LogWarn(string s) { if (!Repeated("W", s)) SinkWarn(s); }
		public static void LogError(string s) { if (!Repeated("E", s)) SinkError(s); }

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
						LogInfo("[" + p.Name + "] installed");
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
