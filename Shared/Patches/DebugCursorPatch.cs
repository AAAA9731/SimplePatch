using System;
using evt;
using HarmonyLib;
using UnityEngine;
using XX;

namespace SimplePatch
{
	// Patch "DebugCursor": while the in-game debug menu (EvDebugger / its event-line debugger) is open,
	// force the OS mouse cursor to be visible, without having to open the normal menu UI first.
	public sealed class DebugCursorPatch : IPatch
	{
		public string Name { get { return "DebugCursor"; } }
		public string Description { get { return "Force the mouse cursor visible while the debug menu is open."; } }

		public bool Install(Harmony h)
		{
			if (AccessTools.Method(typeof(EvDebugger), "changeActivate", null, null) == null
				|| AccessTools.Method(typeof(EvDebugger), "runIRD", null, null) == null)
			{
				PatchHost.LogError("EvDebugger.changeActivate/runIRD not found; DebugCursor disabled.");
				return false;
			}
			h.CreateClassProcessor(typeof(PatchChangeActivate)).Patch();
			h.CreateClassProcessor(typeof(PatchRun)).Patch();
			return true;
		}
	}

	internal static class DebugCursorCore
	{
		private static bool forced;

		// Re-evaluated on every debug menu state change and every frame the debugger runs.
		internal static void Sync(EvDebugger d, bool per_frame)
		{
			bool on = d != null && (d.isActive() || d.isELActive());
			if (on != forced)
			{
				forced = on;
				try
				{
					CURS.setTemporaryNormalMouse(on);
				}
				catch (Exception ex)
				{
					PatchHost.LogError("DebugCursor: " + ex);
				}
			}
			if (on && per_frame)
			{
				// CURS.fine()/fineMouse2() keep hiding the cursor; keep it shown.
				Cursor.visible = true;
				Cursor.lockState = CursorLockMode.None;
			}
		}
	}

	[HarmonyPatch(typeof(EvDebugger), "changeActivate")]
	internal static class PatchChangeActivate
	{
		private static void Postfix(EvDebugger __instance)
		{
			DebugCursorCore.Sync(__instance, false);
		}
	}

	[HarmonyPatch(typeof(EvDebugger), "runIRD")]
	internal static class PatchRun
	{
		private static void Postfix(EvDebugger __instance)
		{
			DebugCursorCore.Sync(__instance, true);
		}
	}
}
