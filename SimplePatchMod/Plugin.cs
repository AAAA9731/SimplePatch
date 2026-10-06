using BepInEx;
using BepInEx.Configuration;
using BepInEx.Unity.Mono;
using SimplePatch;
using System;
using System.Collections.Generic;

namespace SimplePatchMod
{
	// BepInEx 6 front-end. All logic lives in Shared/.
	[BepInPlugin("local.aic.simplepatch", "SimplePatch", "1.0.0")]
	public class Plugin : BaseUnityPlugin
	{
		private void Awake()
		{
			foreach (System.Reflection.Assembly a in AppDomain.CurrentDomain.GetAssemblies())
			{
				if (a.GetName().Name == "SimplePatchBoot")
				{
					base.Logger.LogInfo("SimplePatchBoot (standalone injector) is already active; skipping.");
					return;
				}
			}
			PatchHost.LogInfo = (s => base.Logger.LogInfo(s));
			PatchHost.LogWarn = (s => base.Logger.LogWarning(s));
			PatchHost.LogError = (s => base.Logger.LogError(s));
			TxLoadCore.SelfTestEnabled = base.Config.Bind<bool>("Debug", "SelfTest", false, "Run a TX_LOAD parse test on every TX reload and log the result.").Value;
			Dictionary<string, bool> on = new Dictionary<string, bool>();
			foreach (IPatch p in PatchHost.Patches)
			{
				on[p.Name] = base.Config.Bind<bool>("Patches", p.Name, true, p.Description).Value;
			}
			PatchHost.IsEnabled = (name => !on.ContainsKey(name) || on[name]);
			PatchHost.InstallAll();
		}
	}
}
