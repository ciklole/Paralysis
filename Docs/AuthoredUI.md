# Authored UI and dialogue voices

The runtime UI has an editable Canvas version. If a prefab is missing or a
view reference is not assigned, the old runtime builder is used as a fallback,
so the game still works while a canvas is being edited.

## Where the canvases live

| Screen | Asset | Sorting order |
| --- | --- | --- |
| Main menu | `Assets/Scenes/MainMenu.unity` (`MainMenuCanvas`) | 100 |
| End of demo | `Assets/Scenes/End.unity` (`EndCanvas`) | 100 |
| Pause menu | `Assets/Resources/Paralysis/PauseMenu.prefab` | 450 |
| Dialogue | `Assets/Resources/Paralysis/DialogueCanvas.prefab` | 300 |
| Loading / chapter intro | `Assets/Resources/Paralysis/TransitionCanvas.prefab` | 600 |
| Objective HUD | `Assets/Resources/Paralysis/ObjectiveCanvas.prefab` | 200 |
| Instruction toast | `Assets/Resources/Paralysis/ToastCanvas.prefab` | 500 |
| Mission log | `Assets/Resources/Paralysis/QuestLogCanvas.prefab` | 400 |

The gameplay prefabs are loaded from `Resources/Paralysis` at runtime by
`DialogueSystem`, `PauseManager`, `SceneTransitionManager`, `ObjectiveManager`,
`ToastUI` and `QuestLogUI`. The MainMenu canvas is part of the scene.

## Editing

- Open the prefab (or the MainMenu scene) and edit text, fonts, colours,
  images, anchors and sizes in the normal Canvas editor.
- Keep the `*View` component references filled. They tell the gameplay code
  which objects to update.
- `applyStyles` is off by default, so the Canvas owns the font and colour.
  Turn it on to use the style configured on the gameplay component instead.
- Extra images, portraits or decorative objects are safe; the code only
  touches the referenced labels, panels and buttons.

### Button sizes and label fonts

Sizes and font sizes set in the Canvas are kept at runtime:

- `MenuButton` remembers the label's font size from the Canvas. The shared
  style no longer overwrites it. Enable `useStyleFontSize` on a `MenuButton`
  if you want that one button to follow `MenuButtonStyle.normalFontSize`
  instead. Hover growth is scaled to the authored size.
- Buttons that live inside a `VerticalLayoutGroup`, `HorizontalLayoutGroup`
  or `GridLayoutGroup` are laid out by that group, which normally snaps their
  RectTransform size back. Resizing such a button now copies the new size into
  its `LayoutElement` (`preferred` and `min` size, flexible 0), so the layout
  keeps the size you chose.
- If a layout-controlled button has no `LayoutElement` yet, select it and run
  `Paralysis > UI > Keep Selected Button Sizes` once. The same command also
  re-syncs a size if it was changed through the Inspector instead of the
  Canvas handles.

### What is dynamic

The Canvas owns the look; some labels are filled at runtime and their prefab
text is replaced when the game runs:

- Objective body: set by `ObjectiveManager` / `StoryDirector`.
- Instruction toast: set by `ToastUI.Show(...)` from the calling component.
- Dialogue speaker and body: set by the `DialogueSequence` line.
- Mission-log body: generated from the live objectives and shopping list.

The authored header/title labels (`OBJECTIVE`, `MISSION LOG`) are kept unless
they are empty. To change the dynamic wording, edit the dialogue sequence
asset or the calling component's serialized message fields, not the prefab
text.

### Objective HUD

`ObjectiveView` references the panel, header and body text. `ObjectiveManager`
still owns the objective list. Completed objectives use `completedColor`.

### Instruction toast

`ToastView` references the panel and text. `ToastUI.Show(message, duration)`
fills the text and shows the panel. The optional `CanvasGroup` can be used for
fading; the runtime sets alpha to 1 and leaves raycasts off.

### Mission log

`QuestLogView` references the canvas, panel, title, body text and optional
scroll view. The body uses rich text and a `ContentSizeFitter`, so the existing
objective/shopping text still scrolls. `QuestLogUI` opens and closes the whole
canvas, so keep the prefab root inactive when saving it.

## Rebuilding

Editor menu: `Paralysis > Setup > Build Authored UI`

Request command:

```text
buildauthoredui
```

This rebuilds the generated prefabs and the MainMenu Canvas. It never opens,
saves or modifies `Store.unity` or `City.unity`. Existing edits to the
generated canvases are replaced, so duplicate a prefab before making
experimental changes.

The end screen is a separate scene. Build it once with
`Paralysis > Setup > Build End Screen (safe)`, or rebuild it with
`Paralysis > Setup > Rebuild End Screen`. The request command is `buildend`.
See `Docs/Ending.md` for the scene layout, animation and music.

The builder creates its temporary scenes additively. In batch mode it first
opens MainMenu (or Home) because Unity cannot add a scene to an untitled one.

## Dialogue text and per-line voice tracks

Conversations live in `Assets/Dialogue/*.asset` (`DialogueSequence`). The
`DialogueDatabase` in `Assets/Resources/Paralysis/DialogueDatabase.asset`
maps conversation ids to those assets, so the protected story scenes do not
need editing.

Each line has:

- `Speaker` and `Text` - the visible line.
- `Voice Clip` - an `AudioClip` played when the line appears.
- `Voice Volume` - a 0..1 multiplier.
- `Sound Effect` - optional non-voice sound.

Select a sequence asset to use the custom inspector. It groups the voice track
for each line and shows the expected file name.

### Naming convention

For `APT_Opening_01.asset`, line 1 uses:

```text
Assets/Sounds/Speeches/APT_Opening_01_01.wav
Assets/Sounds/Speeches/APT_Opening_01_02.wav
...
```

`.mp3` and `.ogg` are also accepted. The line number is 1-based and padded to
two digits.

### Assigning

- Drag an `AudioClip` directly into `Voice Clip` in the custom inspector.
- Or drop files into `Assets/Sounds/Speeches` using the naming convention,
  then run `Paralysis > Setup > Assign Dialogue Voice Clips`.
- `Assign Dialogue Voice Clips (Overwrite)` replaces existing assignments.
- The custom inspector's `Find In Speeches` button fills one line from the
  matching file.

Replacing a placeholder WAV with a real recording at the same path updates the
clip in place; no reassignment is needed.

The current files are placeholder Windows SAPI voices. Replace them before
release. The voice report is written to `Logs/dialogue_voice_report.txt`.

## Validation

The play-mode smoke test checks that all authored canvases are present and that
every dialogue line has a voice clip. It also checks the post-checkout pause
regression. Run it from the editor request file with `smoke`, or in batch mode:

```text
Unity.exe -batchmode -nographics -projectPath <project> ^
  -executeMethod SmokeTestRunner.Run -paralysisSmoke
```

Results are written to `Logs/smoke_results.txt`.
