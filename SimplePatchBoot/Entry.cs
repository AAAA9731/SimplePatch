using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace SimplePatchBoot
{
	// Called by the native version.dll proxy through mono_runtime_invoke once Assembly-CSharp is loaded.
	public static class Entry
	{
		public static void Start()
		{
			string dir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
			string log = Path.Combine(dir, "log.txt");
			Action<string> w = delegate (string s)
			{
				try { File.AppendAllText(log, DateTime.Now.ToString("HH:mm:ss.fff") + " " + s + "\r\n"); } catch { }
			};
			try
			{
				foreach (Assembly a in AppDomain.CurrentDomain.GetAssemblies())
				{
					if (a.GetName().Name == "SimplePatchMod")
					{
						w("BepInEx build of SimplePatchMod is active; standalone injector stays idle.");
						return;
					}
				}
				SimplePatch.PatchHost.LogInfo = w;
				SimplePatch.PatchHost.LogWarn = delegate (string s) { w("WARN " + s); };
				SimplePatch.PatchHost.LogError = delegate (string s) { w("ERROR " + s); };
				SimplePatch.TxLoadCore.SelfTestEnabled = File.Exists(Path.Combine(dir, "selftest"));
				HashSet<string> off = ReadDisabled(Path.Combine(dir, "patches.txt"));
				SimplePatch.PatchHost.IsEnabled = name => !off.Contains(name);
				SimplePatch.PatchHost.InstallAll();
			}
			catch (Exception ex)
			{
				w("ERROR " + ex);
			}
		}

		// patches.txt: one "Name=false" per line disables that patch; everything else (and a missing file) means enabled.
		private static HashSet<string> ReadDisabled(string path)
		{
			HashSet<string> off = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
			if (!File.Exists(path))
			{
				return off;
			}
			foreach (string raw in File.ReadAllLines(path))
			{
				string line = raw.Trim();
				int eq = line.IndexOf('=');
				if (eq <= 0 || line[0] == '#' || line[0] == ';')
				{
					continue;
				}
				string v = line.Substring(eq + 1).Trim();
				if (v.Equals("false", StringComparison.OrdinalIgnoreCase) || v == "0" || v.Equals("off", StringComparison.OrdinalIgnoreCase))
				{
					off.Add(line.Substring(0, eq).Trim());
				}
			}
			return off;
		}
	}
}
