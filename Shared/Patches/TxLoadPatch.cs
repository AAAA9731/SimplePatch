using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text.RegularExpressions;
using Better;
using evt;
using HarmonyLib;
using nel;
using XX;

namespace SimplePatch
{
	// Patch "TxLoad": adds the event command  TX_LOAD <<<EOF [family] ... EOF;
	// The heredoc body is plain localization "tx*.txt" syntax and is fed to the game's own parser (TX.readTexts).
	public sealed class TxLoadPatch : IPatch
	{
		public string Name { get { return "TxLoad"; } }
		public string Description { get { return "Adds the event command TX_LOAD <<<EOF ... EOF; (inline tx*.txt text)."; } }

		public bool Install(Harmony h)
		{
			if (!TxLoadCore.Prepare())
			{
				return false;
			}
			h.CreateClassProcessor(typeof(PatchCacheRead)).Patch();
			h.CreateClassProcessor(typeof(PatchRead)).Patch();
			h.CreateClassProcessor(typeof(PatchSceneGameAwake)).Patch();
			h.CreateClassProcessor(typeof(PatchReloadTx)).Patch();
			PatchHost.LogInfo("TX_LOAD installed. Command: TX_LOAD <<<EOF [family] ... EOF;");
			return true;
		}
	}

	internal static class TxLoadCore
	{
		public static bool SelfTestEnabled;

		private static readonly List<KeyValuePair<string, string>> Registry = new List<KeyValuePair<string, string>>();
		private static MethodInfo MiReadTexts;
		private static readonly Regex RegSection = new Regex(@"^/\*\s*_{3}\s*(\S+)", RegexOptions.Compiled);

		public static bool Prepare()
		{
			MiReadTexts = AccessTools.Method(typeof(TX), "readTexts", null, null);
			if (MiReadTexts == null)
			{
				PatchHost.LogError("TX.readTexts not found; TX_LOAD disabled.");
				return false;
			}
			return true;
		}

		// Consumes the heredoc from the reader, registers its body, and applies it.
		internal static void Handle(CsvReader r)
		{
			string head = (r != null) ? r._1 : null;
			if (head == null || !head.StartsWith("<<<", StringComparison.Ordinal))
			{
				PatchHost.LogWarn("TX_LOAD needs a here document: TX_LOAD <<<EOF");
				return;
			}
			string term = head.Substring(3);
			if (term.Length > 0 && term[0] == '\'')
			{
				term = term.Substring(1);
			}
			if (term.Length > 0 && term[term.Length - 1] == '\'')
			{
				term = term.Substring(0, term.Length - 1);
			}
			term += ";";
			string fam = (r.clength >= 3) ? r.getIndex(2) : null;
			if (fam == "*" || fam == "")
			{
				fam = null;
			}
			List<string> lines = new List<string>();
			bool closed = false;
			while (r.readCorrectly())
			{
				string s = r.getLastStrS().ToString() ?? "";
				if (s == term)
				{
					closed = true;
					break;
				}
				lines.Add(s);
			}
			if (!closed)
			{
				PatchHost.LogWarn("TX_LOAD: missing terminator '" + term + "'");
			}
			string body = string.Join("\n", lines.ToArray());
			KeyValuePair<string, string> item = new KeyValuePair<string, string>(fam, body);
			bool found = false;
			for (int i = 0; i < Registry.Count; i++)
			{
				if (Registry[i].Key == fam && Registry[i].Value == body)
				{
					found = true;
					break;
				}
			}
			if (!found)
			{
				Registry.Add(item);
			}
			Apply(fam, body);
		}

		// Entering a game (new game / loading a save) starts from a clean slate: forget every TX_LOAD
		// override and reload the original texts, which are otherwise still sitting overwritten in memory.
		internal static void ResetForNewGame()
		{
			if (Registry.Count == 0)
			{
				return;
			}
			int n = Registry.Count;
			Registry.Clear();
			try
			{
				TX.reloadTx(false);
			}
			catch (Exception ex)
			{
				PatchHost.LogError("TX_LOAD reset: reloadTx failed: " + ex);
			}
			PatchHost.LogInfo("TX_LOAD: cleared " + n.ToString() + " override(s) on entering game.");
		}

		internal static void ReapplyAll()
		{
			for (int i = 0; i < Registry.Count; i++)
			{
				Apply(Registry[i].Key, Registry[i].Value);
			}
		}

		private static void Apply(string fam, string body)
		{
			BDic<string, TX.TXFamily> all = TX.getWholeTextFamilyObject();
			if (all == null)
			{
				return;
			}
			string[] keys = ExtractKeys(body);
			int n = 0;
			foreach (KeyValuePair<string, TX.TXFamily> kv in all)
			{
				if (fam != null && kv.Key != fam)
				{
					continue;
				}
				// readTexts appends to an existing entry, so clear first to make re-runs idempotent.
				for (int i = 0; i < keys.Length; i++)
				{
					TX.getTX(keys[i], false, false, kv.Value).setContent("");
				}
				MiReadTexts.Invoke(null, new object[] { body, kv.Value });
				n++;
			}
			PatchHost.LogInfo("TX_LOAD: " + keys.Length.ToString() + " key(s) -> " + n.ToString() + " family(ies)" + ((fam != null) ? (" [" + fam + "]") : ""));
		}

		private static string[] ExtractKeys(string body)
		{
			List<string> keys = new List<string>();
			string[] lines = body.Split('\n');
			for (int i = 0; i < lines.Length; i++)
			{
				string t = lines[i].Trim();
				if (t.StartsWith("&&", StringComparison.Ordinal))
				{
					int e = 2;
					while (e < t.Length && (char.IsLetterOrDigit(t[e]) || t[e] == '_'))
					{
						e++;
					}
					if (e > 2)
					{
						keys.Add(t.Substring(2, e - 2));
					}
				}
				else
				{
					Match m = RegSection.Match(t);
					if (m.Success)
					{
						keys.Add(m.Groups[1].Value);
					}
				}
			}
			return keys.ToArray();
		}

		internal static void RunSelfTest()
		{
			PicLoadCore.RunSelfTest();
			try
			{
				CsvReader r = new CsvReader("TX_LOAD <<<EOF\n&&modtest hello world\n/* ___ modtest2 ___ */\nline1\nline2\nEOF;", CsvReader.RegOnlySpace, false);
				r.read();
				PatchHost.LogInfo("SelfTest: cmd=" + r.cmd + " _1=" + r._1);
				Handle(r);
				PatchHost.LogInfo("SelfTest: modtest=[" + TX.Get("modtest", "<missing>") + "] modtest2=[" + TX.Get("modtest2", "<missing>").Replace("\n", "|") + "]");
			}
			catch (Exception ex)
			{
				PatchHost.LogError("SelfTest failed: " + ex);
			}
		}
	}

	[HarmonyPatch(typeof(NelM2DEventListener), "EvtCacheRead")]
	internal static class PatchCacheRead
	{
		private static bool Prefix(CsvReader rER, ref int __result)
		{
			if (rER.cmd != "TX_LOAD")
			{
				return true;
			}
			TxLoadCore.Handle(rER);
			__result = 1;
			return false;
		}
	}

	[HarmonyPatch(typeof(NelM2DEventListener), "EvtRead")]
	internal static class PatchRead
	{
		private static bool Prefix(StringHolder rER, ref bool __result)
		{
			if (rER.cmd != "TX_LOAD")
			{
				return true;
			}
			TxLoadCore.Handle(rER as CsvReader);
			__result = true;
			return false;
		}
	}

	[HarmonyPatch(typeof(SceneGame), "Awake")]
	internal static class PatchSceneGameAwake
	{
		private static void Prefix()
		{
			TxLoadCore.ResetForNewGame();
		}
	}

	[HarmonyPatch(typeof(TX), "reloadTx")]
	internal static class PatchReloadTx
	{
		private static void Postfix()
		{
			TxLoadCore.ReapplyAll();
			if (TxLoadCore.SelfTestEnabled)
			{
				TxLoadCore.RunSelfTest();
			}
		}
	}
}
