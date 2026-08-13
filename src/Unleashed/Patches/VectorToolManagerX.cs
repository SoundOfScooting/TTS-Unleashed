using System.Runtime.CompilerServices;
using Unleashed.Settings;
using static VectorToolManager;

namespace Unleashed.Patches;

[HarmonyPatch]
static class VectorToolManagerX
{
	[ModuleInitializer]
	internal static void Initializer()
		=> Events.OnStartConnected += OnStartConnected;
	static void OnStartConnected()
		=> Wait.Frames(UpdateUI);

	[Setting]
	static readonly Setting<bool> EnableDrawPixel = new()
	{
		MigrateFrom = [
			("General", "Enable Pixel Draw"), // 0.1.0
		],
		Section     = Section.Controls,
		Key         = "Enable Pixel Draw",
		Default     = true,
		Description =
			"""
			Fully implements the unfinished pixel draw tool, an apparent vector-based rework of the removed pixel paint tool.
			It is located under the draw toolbar between the circle and erase tools.
			Each pixel drawn is one vector line.
			NOTE: If disabled, the tool is not added to GUI but is still accessible with the console command `tool_vector_pixel`.
			""",
		OnChanged = value => UpdateUI(),
	};

	static List<VectorEraseData> EraseBuffer = [];
	static bool EraseCancelled;
	readonly struct VectorEraseData(VectorDrawData drawData)
	{
		public readonly VectorDrawData drawData = drawData;
		public readonly bool      wasAttached   = drawData.attached;
		public readonly int       sortingOrder  = drawData.line.sortingOrder;
		public readonly Vector3[] positions     = drawData.line.GetPositions();
//			for (var i = 0; i < positions.Length; i++)
//				positions[i] = drawData.line.transform.TransformPoint(positions[i]);
	}

	public static void UpdateUI()
	{
		if (!NetworkUI._instance || !NetworkUI.Instance.GUIConnected.activeInHierarchy)
			return;

		var DrawT = NetworkUI.Instance.GUIConnected.transform
			.Find("# Pointer Mode/Anchor/Grid/02 Draw");
		var ui = DrawT.GetComponent<UIPointerMode>();

		int GetIndex(GameObject go)
			=> ui.ExpandButtonStructs.FindIndex(expand => expand.Button == go);
		void AddX(GameObject go, float dx)
		{
			var i = GetIndex(go);
			var expand = ui.ExpandButtonStructs[i];
			expand.StartlocalPosition.x += dx;
			ui.ExpandButtonStructs[i] = expand;
		}

//		var DrawPen    = DrawT.Find("Scroll View/01 Pen"   ).gameObject; // +56
//		var DrawLine   = DrawT.Find("Scroll View/02 Line"  ).gameObject; // +56
//		var DrawBox    = DrawT.Find("Scroll View/03 Box"   ).gameObject; // +56
//		var DrawCircle = DrawT.Find("Scroll View/04 Circle").gameObject; // +56
		var DrawPixel  = DrawT.Find("Scroll View/04 Pixel" ).gameObject; // +56
		var DrawErase  = DrawT.Find("Scroll View/05 Erase" ).gameObject; // +56
		var DrawColor  = DrawT.Find("Scroll View/06 Color" ).gameObject; // +56
		var DrawDelete = DrawT.Find("Scroll View/Delete"   ).gameObject; // +44

		var enabled = EnableDrawPixel.Value;
		if (!enabled)
			Pointer.VectorTools.Remove(PointerMode.VectorPixel);
		else if (!Pointer.VectorTools.Contains(PointerMode.VectorPixel))
			Pointer.VectorTools.Insert(Pointer.VectorTools.IndexOf(PointerMode.VectorErase), PointerMode.VectorPixel);

		var index = GetIndex(DrawPixel);
		if (enabled && (index < 0))
		{
			ui.ExpandButtonStructs.Add(new()
			{
				Button             = DrawPixel,
				ButtonTransform    = DrawPixel.transform,
				StartlocalPosition = ui.ExpandButtonStructs[GetIndex(DrawErase)].StartlocalPosition,
			});
			AddX(DrawErase,  56f);
			AddX(DrawColor,  56f);
			AddX(DrawDelete, 56f);
		}
		if (!enabled && (index >= 0))
		{
			ui.ExpandButtonStructs.RemoveAt(index);
			AddX(DrawErase,  -56f);
			AddX(DrawColor,  -56f);
			AddX(DrawDelete, -56f);
		}
		DrawPixel.SetActive(enabled);
		if (enabled)
			DrawPixel.transform.Find("Sprite").GetComponent<UISprite>().color = Color.white;
	}

	[HarmonyPrefix]
	[HarmonyPatch(typeof(Pointer), nameof(Pointer.IsVectorTool))]
	static bool IsVectorToolPrefix(PointerMode mode, ref bool __result)
	{
		// allow usage even if not in the toolbar
		if (mode == PointerMode.VectorPixel)
			return (false, __result = true).Item1;
		return true;
	}
	[HarmonyILManipulator]
	[HarmonyPatch(typeof(Pointer), nameof(Pointer.Update))]
	static void PointerUpdateIL(ILContext il)
	{
		var c = new ILCursor(il);
		c.GotoNext(MoveType.Before,
			// else if (CurrentPointerMode == PointerMode.Paint)
			x => x.MatchCall(AccessTools.PropertyGetter(typeof(Pointer), nameof(Pointer.CurrentPointerMode))),
			x => x.MatchLdcI4((int) PointerMode.Paint),
			x => x.MatchBneUn(out _)
		);
		c.Index++;
		c.EmitDelegate(
			PointerMode(PointerMode currentPointerMode)
				=> currentPointerMode == PointerMode.VectorPixel
					? PointerMode.Paint
					: currentPointerMode
		);
	}
	[HarmonyPrefix]
	[HarmonyPatch(typeof(VectorToolManager), nameof(VectorToolManager.UpdateVectorPixel))]
	static bool UpdateVectorPixelReplace(VectorToolManager __instance)
	{
		if (__instance.VectorActionDown() || (__instance.VectorAction() && __instance.drawing && __instance.CheckMove()))
			__instance.StartDrawing(2, loop: true);
		if (__instance.VectorActionUp())
			__instance.EndDrawing();
		return false;
	}
	[HarmonyPrefix]
	[HarmonyPatch(typeof(VectorToolManager), nameof(VectorToolManager.UpdateVectorErase))]
	static bool UpdateVectorErasePrefix(VectorToolManager __instance)
	{
		// #todo: somehow preserve overlap order of lines?
			// redrawn lines are all on top, but at least in relative order to each other
		if (__instance.VectorActionDown())
		{
			EraseBuffer    = [];
			EraseCancelled = false;
		}
		if (__instance.VectorActionUp())
			EraseCancelled = false;

		return !EraseCancelled;
	}
	[HarmonyILManipulator]
	[HarmonyPatch(typeof(VectorToolManager), nameof(VectorToolManager.UpdateVectorErase))]
	static void UpdateVectorEraseIL(ILContext il)
	{
		var c = new ILCursor(il);
		var found = 0;
		while (c.TryGotoNext(MoveType.Before,
			// RPCRemoveLine(drawnLine.Key);
			x => x.MatchCall(AccessTools.Method(typeof(VectorToolManager), nameof(VectorToolManager.RPCRemoveLine)))
		)){
			found++;
			c.Emit(OpCodes.Ldloc_2);
			c.EmitDelegate(
				void(KeyValuePair<uint, VectorDrawData> drawnLine)
					=> EraseBuffer.Add(new(drawnLine.Value))
			);
			c.Index += 3; // annoying
		}
		if (found != 2)
			Main.Log.LogWarning($"{nameof(VectorToolManagerX)}.{nameof(UpdateVectorEraseIL)} expected 2, found {found}");
	}

	[HarmonyILManipulator]
	[HarmonyPatch(typeof(VectorToolManager), nameof(VectorToolManager.LateUpdate))]
	static void LateUpdateIL(ILContext il)
	{
		var c = new ILCursor(il);
		c.GotoNext(MoveType.After,
			// if (zInput.GetButtonDown("Tap") && drawing)
			x => x.MatchLdstr("Tap"),
			x => x.MatchLdcI4(0),
			x => x.MatchCall(AccessTools.Method(typeof(zInput), nameof(zInput.GetButtonDown)))
		);
		c.Emit(OpCodes.Ldarg_0);
		c.EmitDelegate(
			bool(bool tapDown, VectorToolManager __instance) =>
			{
				if (tapDown && (__instance.pointerMode == PointerMode.VectorErase) && __instance.VectorAction() && !EraseCancelled)
				{
					EraseCancelled = true;

					List<LineNetworkData> lines = [];
					foreach (var erased in EraseBuffer.OrderBy(x => x.sortingOrder))
					{
						var drawData = erased.drawData;
						if (drawData.attached != erased.wasAttached)
							continue; // attached was destroyed
						lines.Add(new(
							__instance.GetGUID(),
							drawData.playerSteamID,
							drawData.attached,
							erased  .positions,
							drawData.color,
							drawData.thickness,
							drawData.rotation,
							drawData.loop,
							drawData.square
						));
						// __instance.RPC(RPCTarget.Others, __instance.RPCAddLine, lineNetworkData);
						// __instance.RPCAddLine(lineNetworkData);
					}
					if (lines.Count > 0)
					{
						__instance.RPC(RPCTarget.Others, __instance.RPCAddLines, lines);
						__instance.RPCAddLines(lines);
					}
				}
				return tapDown;
			}
		);

		ILLabel skipLabel = null;
		c.GotoNext(MoveType.After,
			// if (zInput.GetButtonDown("Tap") && drawing)
			x => x.MatchLdarg(0),
			x => x.MatchLdfld(AccessTools.Field(typeof(VectorToolManager), nameof(VectorToolManager.drawing))),
			x => x.MatchBrfalse(out skipLabel)
		);
		c.Emit(OpCodes.Ldarg_0); // could instead emit before "Tap" check
		c.EmitDelegate(
			bool(VectorToolManager __instance)
				=> __instance.pointerMode == PointerMode.VectorPixel
		);
		c.Emit(OpCodes.Brtrue, skipLabel);
	}
}

