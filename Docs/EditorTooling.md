# Editor tooling

Reference for the editor-side helpers in this project: the request
dispatcher, the acceptance suite and the screenshot runner.

## Request dispatcher

`Assets/Editor/UrpMaterialTools.cs` polls a request file, so the open editor
can be driven from a terminal. Write the command into
`Temp/urp_request.txt`:

```powershell
New-Item -ItemType Directory -Force -Path Temp | Out-Null
Set-Content -Path Temp\urp_request.txt -Value "buildcashier" -NoNewline
```

Unity deletes `Temp/` on exit, so always recreate the folder before writing.

A command only exists after the scripts have actually recompiled. The editor
may consume the request before compilation finishes and answer with
`unknown command 'x'`. To force a recompile, send `importpumpkin` (it calls
`SaveAndReimport`), then wait until
`Library/ScriptAssemblies/Assembly-CSharp-Editor.dll` is newer than the
edited source file.

Available commands:

| Command | Effect |
| --- | --- |
| `audit` | Report material and shader issues |
| `convert` | Convert materials to URP |
| `pipeline` | Make sure the URP pipeline is assigned |
| `fixdefaults` | Fix built-in default materials |
| `buildstore` | Rebuild the store scene - **destroys hand edits**, see below |
| `buildcar` | Rebuild the driving destination |
| `buildcity` | Place vrbn houses along the City streets and sync CarDriving |
| `buildmenu` | Rebuild the main menu config assets |
| `rebuildmenu` | Rebuild the MainMenu scene from scratch |
| `buildauthoredui` | Rebuild the authored UI canvases |
| `buildend` | Rebuild the end screen |
| `buildcashier` | Re-apply the cashier sequence wiring to Store.unity |
| `dumpcheckout` | Dump the checkout dialogue state |
| `dumpbones` | Dump the procedural rig bones |
| `dumpclips` | Dump the importable animation clips |
| `importpumpkin` | Reimport the pumpkin monster FBX (also forces a recompile) |
| `timedmusic` | Rebuild the timed music cues |
| `importdialogue` | Import the dialogue database |
| `assigndialoguevoice` | Assign voices to the dialogue lines |
| `assigndialoguevoiceoverwrite` | Same, but overwrite existing assignments |
| `dumpdriving` | Dump the driving scene state |
| `drivingsyncdry` | Sync the driving scene without the dry run |
| `drivingsynctest` | Sync the driving scene as a dry run |
| `smoke` | Run the acceptance suite in the editor |
| `shot` | Capture the story scenes |
| `menushots` | Capture the menu screenshots |
| `uishots` | Capture the HUD screenshots |
| `shotcashier` | Capture the cashier sequence |
| `newshots` | Capture the lightning flash and third-person camera |
| `all` | Run the material and pipeline fixes |

## Acceptance suite

Headless run (compiles the project and runs every check):

```powershell
& "C:\Program Files\Unity\Hub\Editor\6000.7.0a6\Editor\Unity.exe" `
  -batchmode -nographics -projectPath C:\Users\super\Paralysis `
  -executeMethod SmokeTestRunner.Run -paralysisSmoke `
  -logFile C:\Users\super\Paralysis\Logs\smoke_batch.log
```

Results land in `Logs/smoke_results.txt` as `RESULT: N passed, M failed`.
The suite currently passes 241/241.

`PlayModeSmokeTest` only starts when `-paralysisSmoke` is passed, so the
same command without it can be used to run the screenshot jobs instead.

`Logs/smoke_results.txt` can be left over from an earlier run - check its
timestamp against the run before trusting it.

## Screenshots

See `Docs/Screenshots.md`.

## Store scene rules

- `Assets/Scenes/Store.unity` is the **authored** scene. It is patched
  directly. Never run `StoreSceneBuilder` - it regenerates the scene and
  destroys hand-made edits.
- `CashierSequenceBuilder.Build()` (request command `buildcashier`) is the
  safe way to re-apply the cashier wiring and tuning to the scene.
- The ending trigger defaults live in `CashierInteraction`, so `Store.unity`
  needs no manual trigger edits.
- The pumpkin monster asset (`Assets/Procedural_Materials/pumpkin monster/`)
  ships with **no usable animation clips** - the FBX animation stack is empty.
  The monster is animated procedurally by `HumanoidRig` +
  `MonsterAnimationDriver`. FBX import settings are Human (`animationType: 3`)
  with `clipAnimations: []`.

## Code layout

- `Assets/code/` - runtime gameplay systems (camera, cashier sequence,
  monster, lighting, teleporters, dialogue).
- `Assets/Scripts/` - UI, settings, menu and other framework code.
- `Assets/Editor/` - editor tooling: scene builders, the request dispatcher
  (`UrpMaterialTools`), screenshot runner, smoke test runner.

There are no assembly definitions; everything is in `Assembly-CSharp`
(`Assembly-CSharp-Editor` for editor code).
