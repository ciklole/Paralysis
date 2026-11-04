# Screenshots

The project can capture UI and scene screenshots from the open editor or from
a terminal request file. Files are written to `Logs/`.

## Menu and chapter screenshots

Editor menu:

`Paralysis > Setup > Capture Menu Screenshots`

Request-file command:

```text
menushots
```

This starts Play Mode and captures:

- `Logs/main_menu.png` - the MainMenu scene.
- `Logs/pause_menu.png` - the pause overlay on top of Home.
- `Logs/chapter_intro.png` - the first chapter's intro screen.

The chapter-intro capture uses the first chapter and the default intro style
from `ChapterDatabase` directly. It does not start a new game, so it does not
change story progress.

## Authored HUD screenshots

Editor menu:

`Paralysis > Setup > Capture HUD Screenshots`

Request-file command:

```text
uishots
```

This starts Play Mode and captures the authored canvases:

- `Logs/dialogue.png` - the dialogue box during the Home opening.
- `Logs/hud_toast.png` - the objective HUD plus an instruction toast.
- `Logs/quest_log.png` - the M-key mission log over the Store.

The screenshot run does not create a save or change story progress. The HUD
capture adds a temporary `screenshot_objective` to the live objective list; it
is discarded when Play Mode ends.

## Story-scene screenshots

Editor menu:

`Paralysis > Setup > Capture Story Scene Screenshots` (request command `shot`)

This writes a request for Store, City, CarDriving and Home and captures each
scene after a short wait. The output files are `store.png`, `city.png`,
`car.png` and `home.png`.

## Request format

`ScreenshotRunner` reads `Temp/paralysis_shot.request`. Each non-empty line is:

```text
scene-or-job|wait-seconds|output-file.png
```

Scene names load that scene and capture after the wait. The special jobs are:

```text
MainMenu|2|main_menu.png
Pause|3|pause_menu.png
ChapterIntro|2|chapter_intro.png
Dialogue|4|dialogue.png
HudToast|3|hud_toast.png
QuestLog|3|quest_log.png
```

The editor request `menushots` writes the three menu jobs above; `uishots`
writes the three HUD jobs. The runner sets `Application.runInBackground = true`
so the capture still runs when the editor is not focused.
