# AI in the build: the full record

This folder exists because someone asked how much of this game was made by
an AI agent, and the answer should not be "trust me". Everything here is a
record I can produce: every command the agent sent into the Unity editor,
when it sent it, and the output the editor wrote back.

The agent was OpenCode, running on my machine, from October 10 to November
23, 2026. It had two ways of driving Unity, and both are logged below.

## Channel 1: the request file (editor open on screen)

`Assets/Editor/UrpMaterialTools.cs` polls `Temp/urp_request.txt`. Write a
command in that file and the open editor runs it, then deletes the file.
This is what `Docs/EditorTooling.md` documents.

The agent used this channel 158 times, across 27 distinct commands:

| Command | Times sent | What it does |
| --- | --- | --- |
| `smoke` | 31 | Run the acceptance suite in the live editor |
| `playstate` | 24 | Report whether the editor is in play mode |
| `buildmenu` | 16 | Rebuild the main menu config assets |
| `importpumpkin` | 13 | Reimport the pumpkin monster FBX (also forces a recompile) |
| `dumpclips` | 10 | Dump the importable animation clips |
| `buildcashier` | 9 | Re-apply the cashier sequence wiring to Store.unity |
| `shotcashier` | 9 | Capture the cashier sequence |
| `buildend` | 5 | Rebuild the end screen |
| `audit` | 4 | Report material and shader issues |
| `convert` | 4 | Convert materials to URP |
| `uishots` | 4 | Capture the HUD screenshots |
| `menushots` | 3 | Capture the menu screenshots |
| `importdialogue` | 3 | Import the dialogue database |
| `dumpcheckout` | 3 | Dump the checkout dialogue state |
| `dumpbones` | 3 | Dump the procedural rig bones |
| `stopplay` | 3 | Leave play mode |
| `shot` | 2 | Capture the story scenes |
| `dumpdriving` | 2 | Dump the driving scene state |
| `drivingsyncdry` | 2 | Sync the driving scene without the dry run |
| `buildcar` | 1 | Rebuild the driving destination |
| `buildstore` | 1 | Rebuild the store scene (this destroys hand edits) |
| `drivingsynctest` | 1 | Sync the driving scene as a dry run |
| `buildauthoredui` | 1 | Rebuild the authored UI canvases |
| `assigndialoguevoice` | 1 | Assign voices to the dialogue lines |
| `timedmusic` | 1 | Rebuild the timed music cues |
| `importpumpkingeneric` | 1 | Reimport the pumpkin monster with generic rig |
| `newshots` | 1 | Capture the lightning flash and third-person camera |

Raw form of the command, straight out of the shell history:

```powershell
Set-Content -Path 'C:\Users\super\Paralysis\Temp\urp_request.txt' -Value 'buildcashier' -NoNewline
```

## Channel 2: headless Unity launches

The agent also launched Unity with `-batchmode -nographics` and an
`-executeMethod` entry point, 54 times:

| Entry point | Launches | What it does |
| --- | --- | --- |
| `SmokeTestRunner.Run` | 15 | Headless acceptance suite |
| `CityBuilder.BuildCity` | 14 | Place the vrbn houses along the city streets and save the scene |
| `CityBuilder.DumpCityLayout` | 5 | Write the street/road layout report |
| `AuthoredUIBuilder.BuildAllBatch` | 3 | Rebuild every UI canvas prefab |
| (asset import / URP convert runs) | 3 | Import and convert assets |
| (Windows player builds) | 3 | Package the shipped build |
| `FontSceneApplier.Apply` | 2 | Apply the font to a scene |
| `DrivingSceneSyncTool.DryRun` | 2 | Preview the driving scene sync |
| `SceneProbe.ProbeFromMenu` | 1 | Probe scene state from the menu |
| `CarDriveDestinationBuilder.Build` | 1 | Rebuild the driving destination |
| `UrpMaterialAudit.Audit` | 1 | Material audit |
| `CityBuilder.DumpHeights` | 1 | Dump height data |
| `CityBuilder.LogBounds` | 1 | Log object bounds |
| `DrivingSceneSyncTool.ApplyReverseSyncBatch` | 1 | Copy CarDriving changes back into City |
| `DrivingSceneSyncTool.ApplySyncBatch` | 1 | Copy City changes into CarDriving |

Raw form, straight out of the shell history:

```powershell
Start-Process -FilePath "C:\Program Files\Unity\Hub\Editor\6000.7.0a6\Editor\Unity.exe" `
  -ArgumentList '-batchmode','-nographics','-projectPath','C:\Users\super\Paralysis',`
                '-executeMethod','CityBuilder.BuildCity',`
                '-logFile','C:\Users\super\Paralysis\Logs\citybuild.log'
```

## Day by day

| Date (UTC) | Request-file commands | Headless launches |
| --- | --- | --- |
| 2026-10-25 | 8 (`buildstore`, `buildcar`, `audit`, `smoke`, `shot`) | 11 |
| 2026-10-26 | 51 (`buildmenu` x15, `smoke` x13, `playstate` x6, `convert` x4, ...) | 3 |
| 2026-10-27 | 7 (`uishots` x4, `smoke`, `menushots`, `playstate`) | 4 (`AuthoredUIBuilder.BuildAllBatch` x3) |
| 2026-10-29 | 12 (smoke/playstate) | 0 |
| 2026-10-30 | 2 (smoke) | 0 |
| 2026-11-03 | 10 (`buildend`, `buildmenu`, `smoke`, `playstate`, `timedmusic`) | 0 |
| 2026-11-04 | 64 (`buildcashier` x9, `shotcashier` x9, `importpumpkin` x10, `dumpclips` x10, `playstate` x9, ...) | 1 |
| 2026-11-05 | 4 | 17 (10x `CityBuilder.BuildCity`) |
| 2026-11-06 | 0 | 1 (`CityBuilder.BuildCity`) |
| 2026-11-07 | 0 | 4 (3x `CityBuilder.BuildCity`) |
| 2026-11-10 | 0 | 1 (`DrivingSceneSyncTool.ApplyReverseSyncBatch`) |
| 2026-11-11 | 0 | 9 (smoke, dry runs, sync) |
| 2026-11-12 | 0 | 3 (Windows player builds) |

The two busiest days are Oct 26 (51 commands, 3 launches) and Nov 4
(64 commands, 1 launch). Both are days the agent was actively writing
scripts and running them back to back.

## The complete list

`editor_commands.csv` has all 212 invocations, one per row:

```
timestamp_utc,channel,command,log_file
2026-10-25 21:45,editor-request,audit,
2026-10-25 22:14,editor-request,buildstore,
2026-10-25 22:14,editor-request,buildcar,
2026-10-25 22:15,editor-request,smoke,
...
2026-11-05 04:13,headless-batch,CityBuilder.BuildCity,citybuild.log
```

## Receipts

The editor tools wrote their output to `Logs/`, which is gitignored because
it is large. The small, useful receipts are copied into `receipts/`:

| File | What it shows |
| --- | --- |
| `citybuild_excerpt.txt` | The `-executeMethod CityBuilder.BuildCity` command line and the builder's own output: `CityBuilder: placed 19 houses (saved=True)` and `CityBuilder: driving scene synced=True` |
| `city_commit_counts.txt` | Per-commit prefab counts for `City.unity` and `CarDriving.unity`, broken down by prefab type, with the commands to reproduce each row |
| `city_report.txt` | The street and road layout the city builder computed, with coordinates |
| `city_instances.tsv` | Every prefab instance the builder placed, with transforms |
| `end_build.txt` | The end-screen builder reporting `End scene already exists; left untouched.` |
| `mainmenu_build.txt` | The menu builder listing the five config assets it rebuilt, and `MainMenu scene already exists; left untouched.` |
| `authored_ui_build.txt` | The UI builder listing the six canvas prefabs it generated |
| `driving_sync.txt` | The driving scene sync report |
| `driving_sync_selftest.txt` | The sync tool's self-test result |
| `smoke_results.txt` | The acceptance suite results, ending in `RESULT: 276 passed, 0 failed` |
| `urp_material_audit.txt` | The material audit output |

## What this means for the scenes

Being straight about it, since this is the part that matters:

- `Assets/Scenes/Home.unity` was never opened by the agent. I built the
  apartment myself.
- `Assets/Scenes/End.unity` was never touched. The end-screen builder
  reported `End scene already exists; left untouched.` I built the ending
  myself.
- `Assets/Scenes/Store.unity` was regenerated once by `StoreSceneBuilder`
  on Oct 25 (the request command `buildstore`), and I hand-edited the
  lighting and the prefab placements afterwards. The uncommitted diff in my
  working tree is my manual pass over it.
- `Assets/Scenes/City.unity` is mine. I built the roads, the crossroads,
  the sidewalks, the street lights, the buildings and the car. The agent
  tried to help with it, and what it actually managed was a generator that
  dropped in 19 houses with the wrong models and then a lot of copying from
  one scene into the other. Here is the scene's history by commit, and it is
  checkable:

| Commit | When | What changed in `City.unity` | Who |
| --- | --- | --- | --- |
| `63f8b1f` | Nov 4, 15:03 | The city as I had built it: 17 GameObjects, 110 prefab instances (40 road pieces, 43 street lights, 24 vrbn buildings, the racing car, the bench) | me |
| `327d543` | Nov 4, 17:24 | +11 GameObjects and +14 building meshes placed by hand in the editor, with colliders on them (`Building_00083_LOD_1`, `Building_00009_LOD_0`, `Building_00276_LOD_1` and friends). `CarDriving.unity` is untouched in this commit | me |
| `a2e8101` | Nov 5, 21:14 | +19 house prefabs (`B00004.prefab`, `B00009.prefab`, `B00274.prefab`, ...) dropped in by the generator, and the driving scene synced to match | agent |
| `25a1e9b` | Nov 7, 15:04 | Those same 19 prefabs swapped for direct `LOD_1` mesh references (`Building_00024_LOD_1.fbx` and friends). Same count, different model. This happened after I had to send a screenshot on Nov 7 telling it the first attempt had used the flat prefabs instead of the 3D ones | agent |
| `2859562` | Nov 10, 09:01 | +16 `LOD_1` buildings and +6 street lights, copied in from CarDriving by `DrivingSceneSyncTool.ApplyReverseSyncBatch` so the two scenes would match. I had edited CarDriving myself and asked it to copy that over | agent, copying my own work |
| `4ff1cca` | Nov 10, 18:17 | +80 street lights and +6 buildings that I placed in the editor that afternoon, committed together with the agent's settings work in the same commit. The agent's forward sync then copied the same content into CarDriving, which is why both scenes show identical counts from here on | me, then duplicated by the agent's sync |

  The raw counts for every commit are in
  `receipts/city_commit_counts.txt`, and any row is checkable yourself:

      git show 63f8b1f:Assets/Scenes/City.unity > a.txt
      git show a2e8101:Assets/Scenes/City.unity > b.txt
      Select-String -Path a.txt,b.txt -Pattern 'm_SourcePrefab' | Measure-Object

  The generator's own receipt is in `receipts/citybuild_excerpt.txt`:
  `CityBuilder: placed 19 houses (saved=True)` and `CityBuilder: driving
  scene synced=True`.

  One more thing worth noticing: from `a2e8101` onwards, `City.unity` and
  `CarDriving.unity` have identical prefab counts at every commit, because
  the agent's sync tool copies the environment back and forth between them.
  That is the "copy from one scene into the other" part, and it is what
  the driving-scene commits (`2859562`, `4ff1cca`) are.

  And the street lights are the clearest single piece of evidence that the
  city is mine. All 129 of them are hand-placed. The last 80 are named
  `V Cone LiT (48)` through `V Cone LiT (127)`, which is the naming Unity
  gives when you duplicate an object in the editor, and they sit in pairs
  on both sides of the pavement at irregular intervals rather than in a
  grid. No generator wrote them, and the `CityBuilder` source has no
  street light code in it at all.
- `Assets/Scenes/CarDriving.unity` I authored, and the agent then edited it
  and synced it with the city.
- `Assets/Scenes/MainMenu.unity` I authored, and the agent patched it and
  rebuilt its config assets.

The dialogue in `Assets/Dialogue/` (9 conversation assets, 115 lines) was
never written or edited by the agent. The agent wrote the importer that
reads those assets, and it assigned the voice clips, but the words are mine.

## The design brief I wrote

The other half of the record is on my side of it. Over the session I typed
108 messages, 163,364 characters in total. Eight of those were long
specification documents, 139,643 characters of them, and they are the
actual design of the game in my own words. They are copied verbatim into
`specs/`:

| Spec | Sent | Size | What it specifies |
| --- | --- | --- | --- |
| `2026-10-24_story_dialogue_objectives_spec.md` | Oct 24 21:41 | 46,545 chars | The whole story, the exact dialogue lines, the objectives, the shopping list, the interaction rules, the timing |
| `2026-10-26_main_menu_and_ui_spec.md` | Oct 26 01:07 | 38,173 chars | 12 numbered deliverables: main menu scene, New Game / Continue / Chapter Select / Settings / Quit, pause system, persistent settings, mouse sensitivity, frame-rate cap, transition loading screens, chapter titles, folder layout, inspector-customizable UI |
| `2026-11-04_cashier_transformation_spec.md` | Nov 4 04:37 | 12,680 chars | The cashier transformation sequence: lighting, animation, monster behaviour, camera control |
| `2026-11-10_final_polish_spec.md` | Nov 10 23:24 | 29,746 chars | Final polish, scene sync, graphics settings, docs, release |

The dialogue in the game is in that first spec, word for word. Section 6.2
of it reads:

```
Conversation ID: `STORE_Opening_01`

Line 1:
Speaker: `You`
Text: "Alright. Let's see what we need."

Line 2:
Speaker: `You`
Text: "Bread, milk, eggs... should be easy enough."
```

And that is exactly what `Assets/Dialogue/STORE_Opening_01.asset` contains.
The agent transcribed my words into the asset file. It did not write them.

The second spec opens with a section I wrote called "CRITICAL SAFEGUARDS -
DO NOT BREAK EXISTING CONTENT", which says, in my words:

```
## 2.1 The remodeled store
I have remodeled the store in the City scene.
DO NOT MODIFY THE REMODELED STORE.
```

The city generator itself was my request on Nov 5 at 01:55:

> okay, can u build a city? like there is a vrbn and there are houses, make a
> variety of them, i the city scene. i already put some of them, however,
> make so that the wont tocuh the road because thats where the car will drive

So the division of labor is: I wrote the game as a set of specifications, I
built the city and the apartment and the ending by hand in the editor, the
agent implemented the specifications in code and wrote the tooling, and I
decided what to keep, what to change, and what shipped.

## Where the code stands

- 111 C# files in the project. The agent wrote or edited 110 of them. For
  106 of those the committed version is the agent's own writing; 4 have
  agent edits that are still uncommitted in my working tree
  (`TransitionRequest.cs`, `GameSettings.cs`, `MotionBlurController.cs`,
  `ResolutionGuard.cs`).
- 21 editor tools in `Assets/Editor/`. The agent wrote all 21, including
  `CityBuilder.cs`, `StoreSceneBuilder.cs`, `MainMenuSceneBuilder.cs`,
  `AuthoredUIBuilder.cs`, `EndScreenBuilder.cs`, `CashierSequenceBuilder.cs`,
  `DrivingSceneSyncTool.cs`, `SmokeTestRunner.cs` and the request
  dispatcher `UrpMaterialTools.cs`.
- The one runtime script the agent never touched is
  `Assets/Scripts/UI/ButtonAppearance.cs`.

The agent also wrote the docs in `Docs/`, packaged the Windows build, and
wrote the acceptance suite that the releases were checked against.

## Reproducing the harness

Everything above runs from the repo root, in PowerShell:

```powershell
# 1. drive the open editor
New-Item -ItemType Directory -Force -Path Temp | Out-Null
Set-Content -Path Temp\urp_request.txt -Value "buildcashier" -NoNewline

# 2. run a builder headless
& "C:\Program Files\Unity\Hub\Editor\6000.7.0a6\Editor\Unity.exe" `
  -batchmode -nographics -projectPath $PWD `
  -executeMethod CityBuilder.BuildCity `
  -logFile "$PWD\Logs\citybuild.log"
```

The full `Logs/` folder from my machine (the raw Unity logs, the relay
log, and the screenshots the agent took) is not committed because it is
large. Say the word and I will publish it separately.
