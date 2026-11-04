# City to CarDriving scene sync

`DrivingSceneSyncTool` copies the environment from `Assets/Scenes/City.unity`
into `Assets/Scenes/CarDriving.unity` without touching the driving-only
objects. The City scene is opened additively, merged into the driving scene,
and never saved.

This is deliberately a tool, not a runtime feature. It is meant to be run
after the City environment changes, then reviewed and saved by hand.

## What is preserved

These driving-scene roots are never replaced or deleted:

- `CarDriveSystems`
- `Store Destination`
- `StoryManagers`
- `FirstPersonController`
- `Free Racing Car`

The tool also protects anything that owns a driving or story component
(`CarDriveScene`, `CarDriveDestination`, `CarDriveController`, `CarCamera`,
`FirstPersonController`, `StoryDirector`, `DialogueSystem`,
`ObjectiveManager`, `ShoppingCart`, `QuestLogUI`, `ToastUI`,
`PlayerInteractor`, `CarInteraction`), plus roots whose names contain
`Player`, `CarCamera` or `CarDrive`.

## What is replaced

Every other root in `CarDriving` is treated as environment and replaced with
the environment roots from `City`. Because the sync uses
`EditorSceneManager.MergeScenes`, prefab instances, prefab overrides,
materials, light settings and internal object references keep their original
serialized data. The tool does not rebuild meshes or copy materials by hand.

## Editor menu

`Paralysis > Scenes > Driving Sync > Dry Run (report only)`

- Opens both scenes additively.
- Writes `Logs/driving_sync.txt`.
- Lists preserved roots, City environment roots, current CarDriving
  environment roots, and differences (only in City, only in CarDriving,
  transform/child/component differences).
- Closes the scenes again and restores the previously active scene.
- Never saves either scene.

`Paralysis > Scenes > Driving Sync > Apply City -> CarDriving`

- Requires a confirmation dialog.
- Refuses to run in Play Mode.
- Refuses to run when `CarDriving.unity` has unsaved changes.
- Deletes the old environment roots, merges City into CarDriving, removes
  the duplicate City-side protected roots (its own walking player and story
  managers), then saves only `CarDriving.unity`.
- `City.unity` is never saved.

`Paralysis > Scenes > Dump City and CarDriving Hierarchy`

- Writes `Logs/scene_structure.txt` with the full hierarchy and components of
  both scenes.
- Useful for checking what the tool considers environment versus protected.

## Request-file commands

When the editor is already open, the same actions can be triggered from a
terminal by writing a command into `Temp/urp_request.txt`:

```text
dumpdriving      -> DumpHierarchy()
drivingsyncdry   -> DryRun()
drivingsynctest  -> RunSelfTest()
```

Poll until the request file disappears, then read the report in `Logs/`.
Other editor request commands are documented in `Assets/Editor/UrpMaterialTools.cs`.

## Self-test

`drivingsynctest` (or `DrivingSceneSyncTool.RunSelfTest()` from the editor)
does not touch the real scenes. It:

1. Copies `City.unity` and `CarDriving.unity` into
   `Assets/__DrivingSyncTest/`.
2. Runs the full apply path on the copies.
3. Reopens the copied driving scene and verifies each protected root exists
   exactly once.
4. Compares the environment root count with the copied City scene.
5. Writes `Logs/driving_sync_selftest.txt`.
6. Deletes `Assets/__DrivingSyncTest/`.

A passing report looks like:

```text
PASS  sync applies to a copy
  CarDriveSystems: 1 (ok)
  Store Destination: 1 (ok)
  StoryManagers: 1 (ok)
  FirstPersonController: 1 (ok)
  Free Racing Car: 1 (ok)
PASS  each driving root exists exactly once
PASS  environment root count is 27 (expected 27)
```

## Recommended workflow

1. Make environment changes in `City.unity` and save City.
2. Open `CarDriving.unity` and save it, or close it if the tool should open it.
3. Run **Dry Run** and review `Logs/driving_sync.txt`.
4. Run **Apply City -> CarDriving** and confirm.
5. Open CarDriving, check the car, player, destination and story managers are
   still present and correctly placed, then save if needed.
6. Run `drivingsynctest` after changing the tool itself.

## Notes and limits

- The tool is editor-only and lives in `Assets/Editor/DrivingSceneSyncTool.cs`.
- It must not run in Play Mode.
- It will not overwrite a dirty `CarDriving.unity`; save or discard changes
  first.
- It does not modify `City.unity`, `Store.unity`, materials, prefabs or build
  settings.
- The City scene still contains its own walking player and story managers.
  Those duplicates are removed after the merge so the driving scene keeps its
  own copies.
- The self-test intentionally uses asset copies, not in-memory clones, so the
  merge path is exercised exactly as it is in the real apply.
- If the environment root count changes in City, update the expected count in
  the self-test report check if you want an exact assertion; the test prints
  the actual and expected counts.
