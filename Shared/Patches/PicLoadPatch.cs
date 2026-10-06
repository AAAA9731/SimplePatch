using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Better;
using evt;
using HarmonyLib;
using nel;
using PixelLiner;
using UnityEngine;
using XX;

namespace SimplePatch
{
	// Patch "PicLoad": adds the event command  PIC_LOAD <id> <file[.png]> [origin_x origin_y]
	// which loads StreamingAssets/SimplePatch_pic/<file>.png and registers it under <id>, so the normal
	// PIC / PIC_B / PIC_FILL ... commands can show it.
	public sealed class PicLoadPatch : IPatch
	{
		public string Name { get { return "PicLoad"; } }
		public string Description { get { return "Adds the event command PIC_LOAD <id> <file.png> (custom PNG from StreamingAssets/SimplePatch_pic)."; } }

		public bool Install(Harmony h)
		{
			if (!PicLoadCore.Prepare())
			{
				return false;
			}
			h.CreateClassProcessor(typeof(PatchPicCacheRead)).Patch();
			h.CreateClassProcessor(typeof(PatchPicRead)).Patch();
			return true;
		}
	}

	internal static class PicLoadCore
	{
		public const string Folder = "SimplePatch_pic";

		private static FieldInfo FiImgTex;
		private static FieldInfo FiPcAPose;
		private static FieldInfo FiPcOImg;
		private static FieldInfo FiSqFrm;
		private static FieldInfo FiConPc;
		private static FieldInfo FiConImgs;

		private sealed class Entry
		{
			public DateTime stamp;
			public EvImg Img;
		}

		private static readonly Dictionary<string, Entry> Loaded = new Dictionary<string, Entry>();

		public static bool Prepare()
		{
			FiImgTex = AccessTools.Field(typeof(PxlImage), "I");
			FiPcAPose = AccessTools.Field(typeof(PxlCharacter), "APose");
			FiPcOImg = AccessTools.Field(typeof(PxlCharacter), "OImg");
			FiSqFrm = AccessTools.Field(typeof(PxlSequence), "AFrm");
			FiConPc = AccessTools.Field(typeof(EvImgContainer), "OPc");
			FiConImgs = AccessTools.Field(typeof(EvImgContainer), "AImgs");
			if (FiImgTex == null || FiPcAPose == null || FiPcOImg == null || FiSqFrm == null || FiConPc == null || FiConImgs == null)
			{
				PatchHost.LogError("PicLoad: required game fields not found; PicLoad disabled.");
				return false;
			}
			return true;
		}

		internal static void Handle(StringHolder r)
		{
			try
			{
				Load(r);
			}
			catch (Exception ex)
			{
				PatchHost.LogError("PIC_LOAD failed: " + ex);
			}
		}

		private static void Load(StringHolder r)
		{
			string id = r._1;
			string file = r._2;
			if (string.IsNullOrEmpty(id) || string.IsNullOrEmpty(file))
			{
				PatchHost.LogWarn("PIC_LOAD needs: PIC_LOAD <id> <file.png> [origin_x origin_y]");
				return;
			}
			string path = ResolvePath(file);
			if (path == null)
			{
				PatchHost.LogWarn("PIC_LOAD: invalid file name '" + file + "'");
				return;
			}
			if (!File.Exists(path))
			{
				PatchHost.LogWarn("PIC_LOAD: file not found: " + path);
				return;
			}
			if (EV.Pics == null)
			{
				PatchHost.LogWarn("PIC_LOAD: EV.Pics is not ready.");
				return;
			}
			BDic<string, EvImg> dic = (BDic<string, EvImg>)FiConPc.GetValue(EV.Pics);
			DateTime stamp = File.GetLastWriteTimeUtc(path);
			Entry old;
			if (Loaded.TryGetValue(id, out old))
			{
				if (old.stamp == stamp)
				{
					return;
				}
			}
			else if (dic.ContainsKey(id))
			{
				PatchHost.LogWarn("PIC_LOAD: id '" + id + "' is already used by the game; choose another id.");
				return;
			}

			Texture2D tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
			if (!ImageConversion.LoadImage(tex, File.ReadAllBytes(path)))
			{
				UnityEngine.Object.Destroy(tex);
				PatchHost.LogWarn("PIC_LOAD: cannot decode PNG: " + path);
				return;
			}
			tex.filterMode = FilterMode.Bilinear;
			tex.wrapMode = TextureWrapMode.Clamp;
			tex.hideFlags = HideFlags.HideAndDontSave;
			float ox = r.Nm(3, 0f);
			float oy = r.Nm(4, 0f);
			EvImg img = BuildImg(id, tex, ox, oy);

			dic[id] = img;
			List<EvImg> list = (List<EvImg>)FiConImgs.GetValue(EV.Pics);
			if (old != null)
			{
				list.Remove(old.Img);
				UnityEngine.Object.Destroy(((Texture2D)FiImgTex.GetValue(old.Img.PF.ALay[0].Img)));
			}
			list.Add(img);
			Loaded[id] = new Entry { stamp = stamp, Img = img };
			PatchHost.LogInfo("PIC_LOAD: '" + id + "' <- " + path + " (" + tex.width.ToString() + "x" + tex.height.ToString() + ")");
		}

		// Builds the synthetic frame from a generated texture and draws it into a MeshDrawer (no event needed).
		internal static void RunSelfTest()
		{
			if (FiImgTex == null)
			{
				return;
			}
			try
			{
				Texture2D tex = new Texture2D(8, 6, TextureFormat.RGBA32, false);
				EvImg img = BuildImg("selftest", tex, 0f, 0f);
				MeshDrawer md = new MeshDrawer(null, 4, 6);
				md.RotaPF(0f, 0f, 1f, 1f, 0f, img.PF);
				PatchHost.LogInfo("PicLoad SelfTest: frame=" + img.PF.ToString() + " layers=" + img.PF.countLayers().ToString() + " tx=" + (MTRX.getMI(img.PF) != null ? "ok" : "null") + " ver=" + md.getVertexMax().ToString() + " buf=" + img.buffer_w.ToString() + "x" + img.buffer_h.ToString());
				UnityEngine.Object.Destroy(tex);
			}
			catch (Exception ex)
			{
				PatchHost.LogError("PicLoad SelfTest failed: " + ex);
			}
		}

		// StreamingAssets/SimplePatch_pic/<file>; rejects rooted paths and "..".
		private static string ResolvePath(string file)
		{
			if (file.IndexOfAny(Path.GetInvalidPathChars()) >= 0 || Path.IsPathRooted(file) || file.Contains(".."))
			{
				return null;
			}
			if (!file.EndsWith(".png", StringComparison.OrdinalIgnoreCase))
			{
				file += ".png";
			}
			string root = Path.GetFullPath(Path.Combine(Application.streamingAssetsPath, Folder));
			string full = Path.GetFullPath(Path.Combine(root, file));
			if (!full.StartsWith(root + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
			{
				return null;
			}
			return full;
		}

		// PxlCharacter -> PxlPose -> PxlSequence -> PxlFrame -> PxlLayer -> PxlImage(texture), wrapped in an EvImg.
		private static EvImg BuildImg(string id, Texture2D tex, float ox, float oy)
		{
			PxlCharacter pc = new PxlCharacter("simplepatch_pic_" + id, false);
			PxlImage pi = new PxlImage(pc);
			FiImgTex.SetValue(pi, tex);
			pi.type = 0;
			pi.width = tex.width;
			pi.height = tex.height;
			Dictionary<PxlImage.PxlImageId, PxlImage> oimg = new Dictionary<PxlImage.PxlImageId, PxlImage>();
			oimg[pi.idkey] = pi;

			PxlPose pose = new PxlPose(pc);
			pose.title = id;
			pose.width = tex.width;
			pose.height = tex.height;
			PxlSequence sq = new PxlSequence(pose, 0);
			pose.ASq[0] = sq;
			PxlFrame frm = new PxlFrame(sq);
			frm.name = id;
			PxlLayer lay = new PxlLayer(frm);
			lay.name = id;
			lay.type = 0;
			lay.x = ox;
			lay.y = oy;
			lay.Img = pi;
			frm.ALay = new PxlLayer[] { lay };
			FiSqFrm.SetValue(sq, new PxlFrame[] { frm });
			sq.width = tex.width;
			sq.height = tex.height;
			pc.createFromOther(new PxlPose[] { pose }, oimg);
			return new EvImg(id, frm);
		}
	}

	[HarmonyPatch(typeof(NelM2DEventListener), "EvtCacheRead")]
	internal static class PatchPicCacheRead
	{
		private static bool Prefix(CsvReader rER, ref int __result)
		{
			if (rER.cmd != "PIC_LOAD")
			{
				return true;
			}
			PicLoadCore.Handle(rER);
			__result = 1;
			return false;
		}
	}

	[HarmonyPatch(typeof(NelM2DEventListener), "EvtRead")]
	internal static class PatchPicRead
	{
		private static bool Prefix(StringHolder rER, ref bool __result)
		{
			if (rER.cmd != "PIC_LOAD")
			{
				return true;
			}
			PicLoadCore.Handle(rER);
			__result = true;
			return false;
		}
	}
}
