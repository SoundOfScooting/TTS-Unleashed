using static UIGridMenu;

namespace Unleashed.GridMenu;

delegate void OnSpawnEvent(GridButtonOnSpawn @this, Vector3 spawnPos, Action<Vector3> spawn);

sealed class GridButtonOnSpawn : GridButtonComponent
{
	public required OnSpawnEvent OnSpawn;
	public sealed override void InteractiveSpawn(Vector3 spawnPos)
		=> OnSpawn.Invoke(this, spawnPos, base.InteractiveSpawn);
	public sealed override void Spawn(Vector3 spawnPos)
		=> OnSpawn.Invoke(this, spawnPos, base.Spawn);

	public static OnSpawnEvent ShowInput(Func<string, string> ParseSpawnName, string Placeholder = "")
		=> (@this, SpawnPos, Spawn)
		=> UIDialog.ShowInput(
			description: $"Spawn {@this.Name}",
			inputName:   Placeholder,

			leftButtonText: "OK",
			leftButtonFunc: input =>
			{
				if (ParseSpawnName.Invoke(input) is not {} spawnName)
				{
					Chat.LogError("Failed to parse input!");
					return;
				}
				Swap(ref @this.SpawnName, ref spawnName);
					Spawn.Invoke(SpawnPos);
				Swap(ref @this.SpawnName, ref spawnName);
			},
			rightButtonText: "Cancel",
			rightButtonFunc: null
		);
}

