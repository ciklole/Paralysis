# End of demo

The demo ends on its own screen: `Assets/Scenes/End.unity`. It has its own
authored Canvas, flies its words in from alternating sides, and shows a
`BACK TO MAIN MENU` button that uses the same `MenuButton` look and sounds as
the rest of the menus.

## When it appears

The cashier's successful checkout already runs the full sequence:

1. conversation
2. silence
3. transformation into the evil cashier
4. restore to normal
5. `onCheckoutSuccess`

After step 5 the cashier waits `endSceneDelay` (default **2.5 s**) and asks
`SceneFlow` to load the ending. The hook lives in `CashierInteraction` with
code defaults, so `Store.unity` and `City.unity` stay untouched:

| Field | Default |
| --- | --- |
| `goToEndScene` | `true` |
| `endSceneName` | `End` |
| `endSceneDelay` | `2.5` |

Turn `goToEndScene` off (or set `EndsDemo = false` from code) to stay in the
store after checkout. The smoke test does this so it can keep checking the
post-checkout state.

## The scene

`End.unity` contains:

- `Main Camera` - black background, orthographic, with the only `AudioListener`.
- `EventSystem` - shared input for the button.
- `EndCanvas` - sorting order 100, authored in the normal Canvas editor:
  - `Background` - full-screen image using `MainMenuConfig.backgroundColor`.
  - `EndContent/Title` - `THE END`.
  - `EndContent/Line1..Line3` - the fly-in lines.
  - `EndContent/BackToMenu` - `BACK TO MAIN MENU`.
- `EndScreenView` - references for the Canvas objects above.
- `EndScreenController` - the animation and the return-to-menu action.

The button is created with `MainMenuConfig.buttonStyle` and
`buttonTextStyle`, and wired to a `MenuAudio` built from
`MainMenuConfig.audio`, so hover growth, brightness, bolding and the click
sound match the main menu.

### Animation

`EndScreenController` uses unscaled time, so it still runs if the game is
paused:

- the title slides in from the left while scaling up slightly;
- each line alternates left/right and fades in with a stagger;
- the back button fades in and rises into place last.

Every value is editable on the controller in the Inspector: `startDelay`,
`flyDuration`, `lineStagger`, `offscreenDistance`, `titleScaleFrom`,
`buttonDelay`, `buttonFadeDuration` and `buttonRise`.

### Return to main menu

`ReturnToMainMenu()` calls `SceneFlow.GoToMainMenu()`, which shows the normal
loading screen, ends the story session and loads the scene from
`SceneFlowConfig` (default `MainMenu`). If the flow is busy, the controller
falls back to a direct `SceneManager.LoadScene`. The button ignores repeat
clicks.

When the ending starts, the controller also restores `Time.timeScale` to 1
and unlocks the cursor (`Cursor.lockState = None`, `Cursor.visible = true`).
Gameplay leaves the pointer locked, so without this the `BACK TO MAIN MENU`
button could be seen but not clicked.

## Music

The ending has its own cue, `end`, and a scene binding `End -> end` in
`Assets/Resources/Paralysis/MusicDatabase.asset`. Like every other cue it is
data-driven and per-cue editable.

- The cue ships with an empty clip and **Loop** off (one-shot). Drag an ending
  theme into **Clip** when you have one.
- While the clip is empty, the controller fades the current music out
  (`stopMusicWhenEndCueIsEmpty`, 2.5 s) instead of letting the store theme
  continue.
- When a clip is assigned, `PrepareMusic()` plays the `end` cue normally.

## Building the scene

Editor menu:

- `Paralysis > Setup > Build End Screen (safe)` - creates the scene only if it
  does not exist.
- `Paralysis > Setup > Rebuild End Screen` - replaces it after a confirmation.

Request command for the editor request file:

```text
buildend
```

The builder creates its scene additively, saves it, closes it and restores the
previously open scene. It appends `End` to Build Settings if it is missing.
It never opens, saves or modifies `Store.unity` or `City.unity`.

## Editing

Open `Assets/Scenes/End.unity` and change the text, fonts, colours, positions
and sizes directly in the Canvas. Keep the `EndScreenView` references filled:

- `canvas`
- `backgroundImage`
- `titleText`
- `lines` (order is the fly-in order)
- `backButton`
- `menuAudio`

`applyConfigText` is off, so the Canvas owns the wording. Turning it on lets
the controller write its own defaults instead.

## Validation

The play-mode smoke test has an `END` section. It loads the scene, checks the
view, title, three lines, the back button and its label, waits for the
animation, clicks the button and confirms the main menu loaded. It also checks
the `End -> end` music binding and that the ending cue is one-shot.

Run it from the editor request file with `smoke`, or in batch mode:

```text
Unity.exe -batchmode -nographics -projectPath <project> ^
  -executeMethod SmokeTestRunner.Run -paralysisSmoke
```

Results are written to `Logs/smoke_results.txt`.
