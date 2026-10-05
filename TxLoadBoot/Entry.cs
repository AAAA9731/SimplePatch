using System;
using System.IO;
using System.Reflection;

namespace TxLoadBoot
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
					if (a.GetName().Name == "TxLoadMod")
					{
						w("BepInEx build of TxLoadMod is active; standalone injector stays idle.");
						return;
					}
				}
				TxLoadMod.Core.LogInfo = w;
				TxLoadMod.Core.LogWarn = delegate (string s) { w("WARN " + s); };
				TxLoadMod.Core.LogError = delegate (string s) { w("ERROR " + s); };
				TxLoadMod.Core.SelfTestEnabled = File.Exists(Path.Combine(dir, "selftest"));
				TxLoadMod.Core.Install();
			}
			catch (Exception ex)
			{
				w("ERROR " + ex);
			}
		}
	}
}
