# TShock utility plugins

This branch contains two independent plugins for the pinned TShock 1.4.5.8 build.

## AutoRegister

- Creates an account only when the Terraria character name does not already exist.
- Skips creation if the client's UUID is already bound to another account.
- Uses TShock's configured default registration group.
- Requires TShock UUID login to be enabled (`DisableUUIDLogin: false`).
- Does not overwrite or modify existing accounts, including admin/owner accounts.

## AutoTeam

- Automatically places players on a Terraria team after joining.
- Default: Red team (`Team: 1`).
- Default: only players currently on no team are assigned (`OnlyIfNoTeam: true`).
- Players may change team afterward; this plugin does not lock team selection.
- Runtime config is generated at `tshock/AutoTeam.json`.

Team values: `0` none, `1` red, `2` green, `3` blue, `4` yellow, `5` pink.

Build artifacts are produced separately as `AutoRegister.dll` and `AutoTeam.dll` by the `Build TShock Plugins` workflow.
