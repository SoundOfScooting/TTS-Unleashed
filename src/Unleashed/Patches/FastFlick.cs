using Unleashed.Settings;

namespace Unleashed.Patches;

[HarmonyPatch]
static class FastFlick
{
	[Setting]
	static readonly Setting<bool> Enabled = new()
	{
		MigrateFrom = [
			("General", "Enable Fast Flick"), // 0.1.0
		],
		Section     = Section.Controls,
		Key         = "Enable Fast Flick",
		Default     = true,
		Description =
			"""
			Flicking an object when not the host no longer requires two clicks (previously the first click would only highlight the object).
			BUG: This allows you to try (and fail) to flick objects that you are prevented from selecting by Lua scripts.
			""",
	};

	[HarmonyILManipulator]
	[HarmonyPatch(typeof(Pointer), nameof(Pointer.StartLine))]
	static void PointerStartLineIL(ILContext il)
	{
		var c = new ILCursor(il);
		c.GotoNext(MoveType.After,
			// if (HighLightedObjects.Count == 0)
			x => x.MatchLdarg   (0),
			x => x.MatchCall    (() => Pointer.__instance().HighLightedObjects),
			x => x.MatchCallvirt(() => List<NetPhysObject>.__instance().Count)
		);
		c.Emit(OpCodes.Ldarg_2);
		c.EmitDelegate(
			int(int count, NetPhysObject hoverObject)
				=> Network.IsClient && Enabled.Value && (count == 0) && hoverObject
					? 1
					: count
		);
	}
}

