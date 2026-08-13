using static UIGridMenu;

namespace Unleashed.GridMenu;

delegate void OnSpawnEvent(GridButtonOnSpawn @this, Vector3 spawnPos, Action<Vector3> spawn);

sealed class GridButtonOnSpawn : GridButtonComponent
{
	public required OnSpawnEvent OnSpawn;
	public sealed override void InteractiveSpawn(Vector3 spawnPos)
		=> OnSpawn(this, spawnPos, base.InteractiveSpawn);
	public sealed override void Spawn(Vector3 spawnPos)
		=> OnSpawn(this, spawnPos, base.Spawn);

	public static OnSpawnEvent ShowInput(Func<string, string> ParseSpawnName, string Placeholder = "")
		=> (@this, SpawnPos, Spawn)
		=> UIDialog.ShowInput(
			description: $"Spawn {@this.Name}",
			inputName:   Placeholder,

			leftButtonText: "OK",
			leftButtonFunc: input =>
			{
				var origName = @this.SpawnName;
				{
					@this.SpawnName = ParseSpawnName?.Invoke(input);
					if (@this.SpawnName is null)
						Chat.LogError("Failed to parse input!");
					else
						Spawn(SpawnPos);
				}
				@this.SpawnName = origName;
			},
			rightButtonText: "Cancel",
			rightButtonFunc: null
		);
}

