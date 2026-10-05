using BepInEx;
using BepInEx.Configuration;
using BepInEx.Unity.Mono;
using System;

namespace TxLoadMod
{
	// BepInEx 6 front-end. All logic lives in Shared/Core.cs.
	[BepInPlugin("local.aic.txloadmod", "TxLoadMod", "1.0.0")]
	public class Plugin : BaseUnityPlugin
	{
		private void Awake()
		{
			foreach (System.Reflection.Assembly a in AppDomain.CurrentDomain.GetAssemblies())
			{
				if (a.GetName().Name == "TxLoadBoot")
				{
					base.Logger.LogInfo("TxLoadBoot (standalone injector) is already active; skipping.");
					return;
				}
			}
			Core.LogInfo = (s => base.Logger.LogInfo(s));
			Core.LogWarn = (s => base.Logger.LogWarning(s));
			Core.LogError = (s => base.Logger.LogError(s));
			Core.SelfTestEnabled = base.Config.Bind<bool>("Debug", "SelfTest", false, "Run a TX_LOAD parse test on every TX reload and log the result.").Value;
			Core.Install();
		}
	}
}
