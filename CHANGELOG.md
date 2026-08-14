# dev

* Updated to game version 14.2.1 (Hotfix 4)
* Added setting `Chat BBCode`
* Fixed swap/force in color selection
* Added ability to change GUID to object context
* Fixed commands being case-sensitive
<!-- * Re-added Herobrine -->

# v0.3.0

* Updated to game version 14.1.8 (Hotfix 2)
<!-- this space intentionally left blank -->
* Fixed error in Objects -> Tables menu
<!-- this space intentionally left blank -->
* Updated name button `Blindfold`/`Unblindfold` to require admin
* Changed name button `Blindfold`/`Unblindfold` to a right-click option
* Fixed name button `Change Team` not checking permissions
<!-- this space intentionally left blank -->
* Reverted object `Material` context extensions temporarily
* Updated object `Physics` context to not require admin
<!-- this space intentionally left blank -->
* Added support for invalid colors to `/color` (any string starting with `!`)
* Changed commands `/whisper` and `/<color>` to not echo
* Moved `/uz***` commands to sub-commands
<!-- * Removed Herobrine -->

# v0.2.0

* Updated to game version 14.0.3 (Hotfix 2)
* Refactored project internals
<!-- this space intentionally left blank -->
* Renamed and relocated some settings (automatically migrate)
* Fixed ConfigurationManager sanitizing special characters in strings
* Added setting `Menu Holiday`
<!-- this space intentionally left blank -->
* Added multi-line support to object `Name` field
* Extended object `Material` context to more objects (`Mesh Index` and `Card ID` planned)
<!-- this space intentionally left blank -->
* Removed `/uz` prefix from most commands and renamed `/uzcmd` to `/sys`
* Added commands `/whisper`, `/color` and improved `/help`, `/kick`, `/ban`, `/promote`, `/mute`
<!-- this space intentionally left blank -->
* Enabled singleplayer/hotseat without running Steam (fixes the `-nosteam` launch option)
<!-- * Added Herobrine -->

# v0.1.0

* Added version number to the title screen
<!-- this space intentionally left blank -->
* Added `Nickname` setting
* Added `Invert Horizontal/Vertical 3P Controls` and `Block Mouse Panning Over UI` settings
* Added `Enable Fast Commands` setting
* Added `Auto Join Message` setting
<!-- this space intentionally left blank -->
* Added `Specific Card` and `Specific Domino` to objects menu
* Added kickstarter gold objects to objects menu (if you have that reward)
* Added `Edit` action to `Custom Rectangle/Square` in tables menu (clients can supply URL remotely)
* Added `Edit` action to `Custom` in backgrounds menu (clients can supply URL remotely)
<!-- this space intentionally left blank -->
* Added `Start Turns`, `Reverse Turns`, `End Turns` to extra name button context
* Fixed `Pass Turn` (requires modded host and client)
<!-- this space intentionally left blank -->
* Changed color selection to show black lock symbol when not promoted
* Added right click to cancel erasing
* Added ability for clients to add/edit/delete decals
* Added abiltiy for clients to edit custom object URLS using object context `Custom`
* Added global/object context `Stash`/`Draw Stash`
* Added commands `/uzcopylua`, `/uzcmd`

# v0.0.1

* Right clicking `Change Color` opens drop down dialog menu
* Instead of shift-clicking the turn star/end turn button to pass backwards, right click it
* Right click rotation snap to decrement
* Added camera home button (next to rotation snap) that changes starting camera position and camera resetting (press Space)
* `Show Hand` available on all players
* Added UI for pixel draw and renamed config option
* Fixed right click to cancel drawing

# v0.0.0

