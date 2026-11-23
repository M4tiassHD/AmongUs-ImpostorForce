# ImpostorForce

A small BepInEx plugin for Among Us (Steam, PC) that lets the **host** choose to be on the impostor team every game.

Toggle it with **F6** or from a menu on **Delete**. A notification on screen tells you whether it is on or off.

> Unofficial mod. Not affiliated with or endorsed by Innersloth.

## How it works

Role assignment in Among Us is decided by the host. When a game starts, the host runs `RoleManager.SelectRoles`, which hands out roles one player at a time through `PlayerControl.RpcSetRole`.

ImpostorForce patches those two methods with Harmony. While `SelectRoles` is running and the option is on:

1. The first role in the impostor team (Impostor, Shapeshifter, Phantom or Viper) that vanilla gives to someone else is given to **you** instead.
2. The role you would have received is given to **that player**.

It is a swap, not an overwrite. The number of impostors and the special roles in the game are exactly what vanilla picked, so the game's own checks never see an impossible lobby. If vanilla already put you on the impostor team, nothing is changed. If your role call comes before the impostor's, your crew role is handed to the displaced player afterwards.

Because only the host runs `SelectRoles`, the option does nothing when you join someone else's lobby.

## Requirements

- Among Us on Steam, Windows x64. Built against game version `2026.9.29` (shown in game as `v19s`).
- A BepInEx 6 **IL2CPP, win-x64** build.
- Game updates can break the plugin until it is rebuilt against the new version. Check the repository for a newer release.

## Install

1. Close Among Us.
2. Download a BepInEx 6 bleeding-edge IL2CPP build for Windows x64 from <https://builds.bepinex.dev/projects/bepinex_be> (file name `BepInEx-Unity.IL2CPP-win-x64-6.0.0-be.<number>+<hash>.zip`).
3. Extract the zip into the game folder, next to `Among Us.exe` (usually `C:\Program Files (x86)\Steam\steamapps\common\Among Us`).
4. Download `ImpostorForce.dll` from the Releases page, or build it yourself (below), and place it in `BepInEx\plugins`.
5. Start the game from Steam. **The first launch is slow**: a console window opens and BepInEx generates interop files for a few minutes before the game window appears. Later launches are fast.

## Use

1. Host a lobby (Local or Online, Create Game).
2. Press **F6** to turn the option on or off, or press **Delete** and use the "Force me impostor" checkbox. A message on screen confirms the state.
3. Start the game. The option applies at the next game start.

The plugin writes `SelectRoles done: ...` lines to the BepInEx console, which tell you whether a swap happened.

### Settings

`BepInEx\config\local.impostorforce.cfg` is created after the first run:

| Setting | Default | Meaning |
|---|---|---|
| `ForceImpostor` | `false` | Whether the option is on. Saved between sessions. |
| `MenuKey` | `Delete` | Opens and closes the menu. |
| `ToggleKey` | `F6` | Toggles the option and shows a message. |

If another mod already uses Delete for its own menu, change `MenuKey` here.

## Build from source

Requires the [.NET SDK](https://dotnet.microsoft.com/download) (6.0 or newer).

```
dotnet build src -c Release
```

The output is `src\bin\Release\net6.0\ImpostorForce.dll`. Packages are restored from nuget.org and the BepInEx feed (`nuget.config`), including `AmongUs.GameLibs.Steam`, which provides the game's reference assemblies. To target a newer game version, change that package version in `src\ImpostorForce.csproj`.

## Status

First release. Which game version, which BepInEx build and which lobby sizes it has been tested with is not yet recorded here. Please report what you tried in an issue, including the BepInEx console output if something fails.

## Credits

[BepInEx](https://github.com/BepInEx/BepInEx), [Harmony](https://github.com/pardeike/Harmony) and [Il2CppInterop](https://github.com/BepInEx/Il2CppInterop). The project setup follows the one used by [MalumMenu](https://github.com/scp222thj/MalumMenu); no MalumMenu source code is included.

## License

MIT, see [LICENSE](LICENSE).
