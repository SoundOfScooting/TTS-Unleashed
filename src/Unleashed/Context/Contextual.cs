using System.Runtime.CompilerServices;
using Unleashed.Compat;

namespace Unleashed.Context;

[HarmonyPatch]
static class Contextual
{
	public const string LABEL_PADDING = "            ";

	public static void Check(GameObject gameObject, Func<bool> active)
	{
		gameObject.SetActive(false);
		if (active())
			PlayerScript.PointerScript.SetActive(gameObject, true);
	}

	[HarmonyILManipulator]
	[HarmonyPatch(typeof(Pointer), nameof(Pointer.StartContextual))]
	static void StartContextualIL(ILContext il)
	{
		var c = new ILCursor(il);
		c.GotoNext(MoveType.After,
			// SetActive(NetworkInstance.GUIContextualHandReveal, NetworkSingleton<NetworkUI>.Instance.handZoneToReveal != null && NetworkSingleton<NetworkUI>.Instance.handZoneToReveal.TriggerLabel == PointerColorLabel);
			x => x.MatchLdfld(AccessTools.Field(typeof(NetworkUI), nameof(NetworkUI.handZoneToReveal))),
			x => x.MatchCallvirt(AccessTools.PropertyGetter(typeof(HandZone), nameof(HandZone.TriggerLabel))),
			x => x.MatchLdarg(0),
			x => x.MatchLdfld(AccessTools.Field(typeof(Pointer), nameof(Pointer.PointerColorLabel))),
			x => x.MatchCall(AccessTools.Method(typeof(string), "op_Equality"))
		);
		c.EmitDelegate(bool(bool eq) => true);

		c.GotoNext(MoveType.After,
			// SetActive(NetworkInstance.GUIContextualPhysics, Network.isServer && flag);
			x => x.MatchLdfld(AccessTools.Field(typeof(NetworkUI), nameof(NetworkUI.GUIContextualPhysics))),
			x => x.MatchCall(AccessTools.PropertyGetter(typeof(Network), nameof(Network.isServer)))
		);
		c.Previous.Operand = AccessTools.PropertyGetter(typeof(Network), nameof(Network.isAdmin));

		c.GotoNext(MoveType.After,
			// SetActive(NetworkInstance.GUIContextualCustom, Network.isServer && (bool)InfoObject.GetComponent<CustomObject>());
			x => x.MatchLdfld(AccessTools.Field(typeof(NetworkUI), nameof(NetworkUI.GUIContextualCustom))),
			x => x.MatchCall(AccessTools.PropertyGetter(typeof(Network), nameof(Network.isServer)))
		);
		c.Previous.Operand = AccessTools.PropertyGetter(typeof(Network), nameof(Network.isAdmin));

		// #todo: either support or remove Scripting but keep GUID
		c.GotoNext(MoveType.After,
			// SetActive(NetworkInstance.GUIContextualScripting, Network.isServer && !string.IsNullOrEmpty(component.GUID));
			x => x.MatchLdfld(AccessTools.Field(typeof(NetworkUI), nameof(NetworkUI.GUIContextualScripting))),
			x => x.MatchCall(AccessTools.PropertyGetter(typeof(Network), nameof(Network.isServer)))
		);
		c.EmitDelegate(bool(bool isServer) => isServer || API.TRUE_ULTIMATE_POWER);

		// c.RemoveRange(2);
		// c.Index++;
		// c.RemoveRange(2);
		c.GotoNext(MoveType.After,
			// NetworkInstance.GUIContextualMenu.SetActive(value: true);
			x => x.MatchLdarg(0),
			x => x.MatchLdfld(AccessTools.Field(typeof(Pointer), nameof(Pointer.NetworkInstance))),
			x => x.MatchLdfld(AccessTools.Field(typeof(NetworkUI), nameof(NetworkUI.GUIContextualMenu))),
			x => x.MatchLdcI4(1),
			x => x.MatchCallvirt(AccessTools.Method(typeof(GameObject), nameof(GameObject.SetActive)))
		);
		// c.MoveAfterLabels();
		c.Index--;
		c.EmitDelegate(Events.TriggerStartContextual);
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

