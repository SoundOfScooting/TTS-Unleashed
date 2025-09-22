using System.Runtime.CompilerServices;
using Unleashed.Compat;

namespace Unleashed.Context;

[HarmonyPatch]
static class Physics
{
	[HarmonyILManipulator]
	[HarmonyPatch(typeof(Pointer), nameof(Pointer.StartContextual))]
	static void StartContextualIL(ILContext il)
	{
		var c = new ILCursor(il);
		c.GotoNext(MoveType.After,
			// SetActive(NetworkInstance.GUIContextualPhysics, Network.isServer && flag);
			x => x.MatchLdfld(AccessTools.Field(typeof(NetworkUI), nameof(NetworkUI.GUIContextualPhysics))),
			x => x.MatchCall(AccessTools.PropertyGetter(typeof(Network), nameof(Network.isServer)))
		);
		c.Previous.Operand = AccessTools.PropertyGetter(typeof(Network), nameof(Network.isAdmin));
	}

	[ModuleInitializer]
	internal static void Initializer() =>
		RemoteX.RegisterOverrides += (RPCMethods) =>
		{
			// -[Remote(Permission.Server)]
			// +[Remote(Permission.Owner, validationFunction: "Permissions/Contextual")]
			var method = AccessTools.Method(typeof(Pointer), nameof(Pointer.SetPhysics));
			RPCMethods.Set(
				method,
				new(Permission.Owner, validationFunction: RPCMethods.AddFunc(
					method,
					player => player.isServer || (
						API.HostModded &&
						BaseNetworkAttribute.validationFunctions["Permissions/Contextual"](player)
					)
				))
			);
		};
	[HarmonyPrefix]
	[HarmonyPatch(typeof(Pointer), nameof(Pointer.SetPhysics))]
	static bool SetPhysicsPrefix(Pointer __instance, int HoverObjectId, RigidbodyState rigidbodyState, PhysicsMaterialState physicsMaterialState)
	{
		if (API.HostModded)
			return true;

		List<string> guids = [];
		foreach (var npo in __instance.GetSelectedNPOs(HoverObjectId, bAlwaysIncludeSelected: true, bIncludeHeld: true))
		if      (npo.gameObject)
		{
			guids.Add(npo.GUID);
			npo.HighlightNotify(__instance.PointerDarkColour);
		}
		Lua.Execute(
			$"""
			for _,guid in ipairs({ Lua.Table(guids) }) do
				local obj = getObjectFromGUID(guid)
				if obj then
					obj.use_gravity      = { rigidbodyState.UseGravity }
					obj.mass             = { rigidbodyState.Mass }
					obj.drag             = { rigidbodyState.Drag }
					obj.angular_drag     = { rigidbodyState.AngularDrag }
					obj.static_friction  = { physicsMaterialState.StaticFriction }
					obj.dynamic_friction = { physicsMaterialState.DynamicFriction }
					obj.bounciness       = { physicsMaterialState.Bounciness }
				end
			end
			"""
		);
		return false;
	}
}

