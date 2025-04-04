# Unleashed

A utility mod for Tabletop Simulator that makes host-only UI available to promoted players while adding bugfixes and other improvements.

# Installation

Follow installation instructions for BepInEx 5: https://docs.bepinex.dev/articles/user_guide/installation/index.html \
**BepInEx 6 is not supported.** Make sure to check the version on the top right.

Download the latest [release](https://github.com/SoundOfScooting/TTS-Unleashed/releases) of Unleashed or [build](#compilation) one yourself.\
Copy `Unleashed.dll` to the folder `[GAME]/BepInEx/plugins/`.

*(optional)* Download ConfigurationManager for BepInEx 5: https://github.com/BepInEx/BepInEx.ConfigurationManager/releases \
This plugin lets you edit Unleashed's [settings file](#settings-file) while in-game.\
Copy `ConfigurationManager.dll` to `[GAME]/BepInEx/plugins/`.

# Compilation

Unleashed is compiled using .NET 9 targetting netstandard2.0.

Copy or symlink all DLL files mentioned in `[REPO]/Unleashed.csproj` from the `[GAME]/Tabletop_Simulator_Data/Managed` folder to the `[REPO]/libs/` folder.\
Run `dotnet build` from `[REPO]/`. The result will be `[REPO]/bin/Debug/Unleashed.dll`.\
To disable test features, run `dotnet build -p:TRUE_ULTIMATE_POWER=0` instead.

# Feature overview

Requirements for each feature are listed.\
Many features require the client to be an admin *(server host or promoted player)*.\
Some features require both the host and client to be modded.

NOTE: As of right now, singleplayer hotseat games are not tested for compatibility.

## Settings file

The plugin generates a config file `edu.sos.unleashed.cfg` in the folder `[GAME]/BepInEx/config/`.

If you have [ConfigurationManager](#installation) installed, you can edit settings in-game by holding Escape and then pressing F1 *(in order)*.\
To close the settings window, either press the `Close` button or click outside the settings window and press Escape.

### Section [General]

`Nickname`\
A nickname used instead of your Steam display name.\
Nickname changes only take effect when joining a new server.

`Menu Player Color` *(default `Purple`)*\
The color of the cursor on the main menu.

`Initial Player Color` *(default `White`)*\
The initial player color after server creation, or `Choose`/`Dialog` to open the color selection UI.

`Initial Background` *(default `Random`)*\
The initial background after server creation, or `Random` for a random one.

`Initial Table` *(default `Random`)*\
The initial table after server creation, or `Random` for a random one.

`Invert Horizontal 3P Controls` *(default `false`)*\
Swaps controls 'Camera Left' with 'Camera Right' in third-person and top-down view.\
This makes them align with first-person view and mouse panning.

`Invert Vertical 3P Controls` *(default `false`)*\
Swaps controls 'Camera Down' with 'Camera Up' in third-person and top-down view.\
This makes them align with first-person view and mouse panning.

`Block Mouse Panning Over UI` *(default `true`)*\
Blocks 'Camera Hold Rotate' control while hovering over UI.\
This eases right clicking UI elements, but prevents mouse panning over large panels.

`Enable Pixel Draw` *(default `true`)*\
Fully implements the unfinished pixel draw tool, an apparent vector-based rework of the removed pixel paint tool.\
It is located under the draw toolbar between the circle and erase tools.\
Each pixel drawn is one vector line.\
NOTE: If disabled, the tool is not added to GUI but is still accessible with the console command `tool_vector_pixel`.

`Enable Fast Flick` *(default `true`)*\
Flicking an object when not the host no longer requires two clicks *(previously the first click would only highlight the object)*.\
BUG: This allows you to try *(and fail)* to flick objects that you are prevented from selecting by Lua scripts.

`Enable Fast Commands` *(default `true`)*\
If shift is not held down, the 'Help' control instead starts typing a command in chat.\
Best used when 'Help' is bound to `/`.

`Intercept Lua Virus` *(default `true`)*\
Host-only: Intercepts the \"tcejbo gninwapS\" Lua virus before it can spread to any other objects.\
NOTE: This does not actually disinfect objects; consider additionally subscribing to CleanerBlock on the Workshop:\
https://steamcommunity.com/sharedfiles/filedetails/?id=2967684892

`Auto Join Message`\
Message automatically sent in chat when a player joins.\
Leave empty for no message.

`Auto Promote Steam IDs`\
A list of Steam IDs that are automatically promoted when joining your server.\
Invalid IDs do nothing, so you can write comments.

## Objects menu

### Components

Modded host and client: *(new)* Added object `Cards/Specific Card`, which opens a dialog to enter a card name.\
The format is `[symbol][suit]`, where `[symbol]` is one of `A`, `K`, `Q`, `J`, `10` through `2`, and `[suit]` is one of `C`, `D`, `S`, `H`.\
For example: `AS` for Ace of Spades.

Modded host and client: *(new)* Added objects `Chess/Gold/* Gold` and `Dice/Gold/D* Gold` if you have the Kickstarter Gold reward.\
*(Objects cannot be golden if the host doesn't have the Kickstarter Gold reward.)*

Modded host and client: *(new)* Added object `Miscellaneous/Specific Domino`, which opens a dialog to enter a domino type.\
The format is `[top]/[bottom] (material)`, where `[top]` and `[bottom]` are integers 0-6, and `(material)` is optional and one of `Plastic`, `Metal`, `Gold`.\
For example: `3/2 Metal` for a metal domino with 3 on the top and 2 on the bottom.\
*(Objects cannot be golden if the host doesn't have the Kickstarter Gold reward.)*

### Tables

*(new)* Added action `Edit` to `Custom Rectangle` and `Custom Square` that lets you edit the URL as a client or as the host.
*(new)* Added unused table `Round Plastic`.

### Backgrounds

*(new)* Added action `Edit` to `Custom` that lets you edit the URL as a client or as the host.

## Rotation snap

Right clicking will decrement your rotation snap instead of incrementing it.\
*(This was already possible but annoying - you'd have to hold right click and then press left click.)*

## Camera home *(new)*

Controls which hand zone the camera starts at and gets reset to when pressing Space.\
Possible options are `Hand` *(your current seat)*, `Grey` *(the "first" hand zone, usually White's)*, or a particular color *(that color's main hand zone)*.

Left click opens the color selection UI *(to pick `Hand`, click again while the UI is open)*.\
Right click opens a drop-down dialog window to pick a home.

While in the color selection UI, if you right click a color/`Hand`, the camera home isn't changed, but your current camera still gets reset to that zone.

NOTE: If Camera #0 has been saved, it overrides the camera home.

## Turns

Admin-only: The turn star icon next to a player's name that skips their turn can be clicked *(formerly host-only)*.\
Admin-only: If you right click the turn star or end turn button, you will pass the turn to the previous player instead of the next one.

## Name button context menu

The popup list shows "Extra:" options when opened with right click instead of left click.

Extra: Admin-only: *(new)* `Start Turns` is available on all players when turns are disabled *(starting them with that player)*.\
Extra: Admin-only: *(new)* `Stop Turns` and `Reverse Turns` are available on all players when turns are enabled.

BUGFIX: Modded host and client: `Pass Turn` actually works when not promoted.

Admin-only: `Change Color` is available on all players *(formerly host-only)*.\
`Change Color` can be clicked again on the same player to cancel color selection.\
If you right click `Change Color`, a drop-down dialog window to pick a color opens.\
If you hold Shift/Ctrl when clicking `Change Color` on yourself, you will remain seated while picking a color instead of switching to Grey *(see [Color selection](#color-selection))*.

`Change Team` is available on all players *(formerly host-only)*.\
*(new)* `Blindfold`/`Unblindfold` is available on all players.

Admin-only: `Promote`/`Demote` and `Kick` are available on non-host players *(formerly host-only)*.

Admin-only: *(new)* `Server Mute` and `Server Unmute` are available on all players.\
NOTE: If you are the host, `Mute`/`Unmute` now only apply client-side only like they do for clients.

## Color selection

Admin-only: The color Black can be chosen in the UI.\
*(Formerly host-only, but could be bypassed using the console.)*

<table>
	Admin-only: If you hold Shift/Ctrl, colors that are already occupied can be chosen.
	<tr>
		<td>Shift</td><td>Swap the colors of the target and seated players.</td>
	</tr>
	<tr>
		<td>Ctrl</td><td>Force the seated player into color Grey so that the target player can switch to theirs.</td>
	</tr>
</table>

## Pointer

### Grab tool

Modded host and client: *(new)* When rotating held objects, if you hold Ctrl, the objects are rotated around their 3rd axis *(previously only Alt for 2nd axis)*.

### Draw tool

See [Enable Pixel Paint](#section-general).

BUGFIX: Pressing right click to cancel drawing actually erases the line for all players instead of just yourself.

*(new)* Pressing right click while erasing stops erasing and redraws all lines that were erased since you started.\
NOTE: As of right now the overlap order of the redrawn lines is preserved, but the redrawn lines appear above all other lines.

<!-- ### Flick tool

See [Enable Fast Flick](#section-general). -->

### Decal tool

Client players can add/edit/delete decals to the decal list.

## Contextual

### Global context menu

Admin-only: *(new)* `Draw Stash` is available when applicable in the global menu, which draws all objects from a hand stash back to its hand.\
If you hold Ctrl, the target is all hand stashes.\
Otherwise, if you hover over a hand stash, the target is that one.\
Otherwise, the target is your own hand stash.\
If you hold Shift, the objects in the hand stash are instead swapped with the ones in the hand *(renames to `Swap Stash`)*.\
BUG: This feature is not well-behaved with non-card objects.

### Object context menu

Admin-only: *(new)* `Stash` is available on selected objects in hands, which moves them into their hand stashes until redrawn.\
If you hold Shift, the selected objects in the hand are instead swapped with the ones in the hand stash *(renames to `Swap Stash`)*.\
BUG: This feature is not well-behaved with non-card objects.

Modded host and client: `Material` will include `Gold` for chess pieces, dice, and dominoes if you have the Kickstarter Gold reward.\
*(Objects cannot be golden if the host doesn't have the Kickstarter Gold reward.)*

Admin-only: `Custom` context option is available on objects *(formerly host-only)*.\
Clients cannot automatically update matching custom objects.\
`Custom Jigsaw` and `Custom PDF` might work but are not fully supported yet for clients.\
If the host is not modded, `Custom Tile` objects cannot have "Stretch to Aspect Ratio" changed by clients.

`Show Hand` context option is available on objects in any hand, not just your own.\
BUG: This feature is buggy *(just like the regular `Show Hand` button)*.

Admin-only: `Physics` context option is available on objects *(formerly host-only)*.\
BUG: The UI might not update client-side but it does apply server-side.

## Chat commands

### Modified commands

`/help`\
Command list slightly modified.\
`/help -a` *(new)*\
Lists hidden commands in addition to the regular ones.

Admin-only: `/kick <name>`, `/ban <name>`, `/promote <name>` *(formerly host-only)*\
As of right now, the player name is case-insensitive but must include all characters.

Admin-only: `/execute <lua script>` *(formerly host-only)*\
Equivalent to the `lua <lua script>` console command.

### New commands

`/uzhelp`\
Lists new modded commands.

`/uzsettings`\
Reloads the [settings file](#settings-file) from disk.

`/uzlist`\
Lists information about each player: player ID, steam name, modded status *(right now only visible to the host)*.

`/uzloading`, `/uzloading <percent>`\
Resets/sets your loading percentage to any value from 0 to 255 *(only values below 100 display)*.\
This effect is temporary and lasts until the game next changes your loading percentage.

`/uzcopylua`\
Copies the last script executed by the mod to the clipboard.\
This is useful to debug internal errors within the mod.

`/uzcmd <command>`\
Executes a system console command from the current chat tab.\
E.g. `/uzcmd chat_copy` would copy the current tab's text instead of the console's text.

