# PARALYSIS

A short first-person horror story about a late-night grocery run that should have been ordinary.

**PARALYSIS** is a narrative horror demo built in Unity. You wake up in a quiet apartment, turn off the lights, drive through an empty city, and walk into a grocery store that feels wrong the longer you stay. The demo is a complete vertical slice: three chapters, dialogue, objectives, driving, shopping, a checkout scene, and an ending.

---

## Contents

- [Story](#story)
- [Features](#features)
- [Chapters and scenes](#chapters-and-scenes)
- [Controls](#controls)
- [Game systems](#game-systems)
- [Project architecture](#project-architecture)
- [Settings](#settings)
- [Credits](#credits)

---

## Story

You wake up in your apartment with a shopping list and a feeling that something is off. The lights need to go out. The car is waiting outside. The city is quieter than it should be.

What starts as a normal errand becomes a slow descent through empty streets, a too-quiet grocery store, and a cashier who knows more than she should.

The demo follows one continuous night:

1. **The Apartment** — wake up, turn off every light, and leave.
2. **The City** — walk to the car, drive through the empty city, and find the store.
3. **The Store** — collect the groceries on the list, reach the checkout, and see how the night ends.

The story is intentionally short. It is a self-contained demo, not a full-length campaign.

---

## Features

- **Three playable chapters** with a beginning, a middle, and an ending.
- **First-person exploration** with inspection, pickups, and environmental interaction.
- **Drivable car** with wheel physics, a third-person orbit camera, and a first-person view.
- **Screen-space navigation compass** during the driving chapter. The arrow tracks the existing store destination and updates as the car and camera turn.
- **Destination landmark glow** that makes the store readable in the dark without raising the city's overall lighting.
- **Dialogue system** with typewriter text, speaker colors, voice clips, and click-to-advance lines.
- **Objective and quest log UI** with toasts and per-chapter goals.
- **Shopping list and checkout** with a real inventory-style cart, pickup feedback, and two checkout outcomes.
- **Chapter select and progress saving** for returning players.
- **Persistent graphics settings**: mouse sensitivity, frame-rate cap, VSync, resolution, and motion blur.
- **Resolution confirmation guard** with a countdown and automatic revert.
- **URP rendering** with a controlled global motion-blur volume.

---

## Chapters and scenes

| Chapter | Scene(s) | What happens |
| --- | --- | --- |
| Main menu | `MainMenu` | New game, continue, chapter select, settings, quit. |
| 1 — The Apartment | `Home` | Opening dialogue, light switches, inspection, and the first objective. |
| 2 — The City | `City` → `CarDriving` | Walk to the car, drive through the city, follow the compass to the store. |
| 3 — The Store | `Store` | Store opening, grocery collection, checkout, and the cashier sequence. |
| Ending | `End` | End-of-demo text, credits, and return to the main menu. |

`CarDriving` is loaded from `City` when the player enters the car. Reaching the store destination loads `Store`. The checkout sequence loads `End`.

The chapter list is data-driven from `Assets/Resources/Paralysis/ChapterDatabase.asset`. The current demo defines three chapters: **The Apartment**, **The City**, and **The Store**.

---

## Controls

### On foot

| Input | Action |
| --- | --- |
| `W` `A` `S` `D` | Move |
| Mouse | Look |
| `Left Shift` | Sprint |
| `Space` | Jump |
| `Left Ctrl` | Crouch |
| `Right Mouse` | Zoom |
| `F` | Interact, inspect, or pick up |
| `E` | Use the car or talk to the cashier |
| `M` | Open or close the quest log |
| `Esc` | Pause, or close the current menu |
| Left click | Advance dialogue |

### Driving

| Input | Action |
| --- | --- |
| `W` / `S` | Throttle / reverse |
| `A` / `D` | Steer |
| Mouse | Look around the car |
| `E` | Exit the car and return to the city |
| `Esc` | Pause |

### Menus and prompts

| Input | Action |
| --- | --- |
| Mouse | Select menu and settings buttons |
| `Esc` | Back / close |
| `Enter` | Confirm a new resolution |
| `Esc` | Revert a new resolution |

### Developer shortcuts

These are only active when `StoryDirector.enableDebugKeys` is enabled:

| Input | Action |
| --- | --- |
| `F8` | Restart the current story session |
| `F9` | Skip to the store stage |
| `F10` | Fill the shopping cart |

---

## Game systems

### StoryDirector

`StoryDirector` is the persistent story controller. It owns the apartment opening, the lights objective, the city arrival, the store arrival, the shopping beats, and the checkout objective. It also handles the optional debug shortcuts.

It survives scene loads and recreates the persistent managers when a new game starts. Scene names, dialogue, objective text, and timings are serialized fields, so the flow can be tuned in the Inspector without rewriting code.

### Objectives and quest log

`ObjectiveManager` owns the current objective list and exposes `SetObjective`, `CompleteObjective`, `IsDone`, and `HasActive`. The HUD shows the active objective; completed objectives briefly remain visible before fading.

`QuestLogUI` is the full list, opened with `M`. The store chapter can auto-open the log after the opening dialogue as an onboarding hint.

### Dialogue

`DialogueSystem` queues conversations instead of allowing them to overlap. Each line supports a speaker, text, optional portrait, optional voice clip, optional sound effect, speaker color, and a delay before the line.

While dialogue is open it owns input, locks the player, and advances on left click. The dialogue data lives in `Assets/Dialogue` as conversation assets, with fallback lines authored directly on the story components.

### Shopping and checkout

`ShoppingCart` is the authoritative list. Items are matched by stable IDs, not display names, so a display name can be changed safely. `GroceryItem` pickups report to the cart, and the story director reacts to the first pickup and to the completed list.

`CashierInteraction` handles the checkout. A complete cart starts the long checkout conversation and the transformation sequence. An incomplete cart starts a shorter failure conversation and reopens the list. All optional assets, sounds, and timings are guarded, so missing references are skipped instead of throwing.

### Driving

The driving chapter is made of several cooperating components:

- `CarInteraction` — enters the car from the city and starts the driving session.
- `CarDriveSession` — tracks the active driving root and the return-to-city state.
- `CarDriveController` — wheel-collider driving with throttle, reverse, steering, idle braking, and an exit key.
- `CarCamera` — third-person orbit and first-person views, using the shared mouse sensitivity.
- `CarDriveDestination` — the arrival trigger, marker snapping, and destination glow.
- `CarNavigationGuide` — a screen-space compass that points at the destination, shows the distance, and fades out when the store is reached or the objective is complete.

The driving scene is intentionally dark. The destination is made readable with a local emissive marker/beam and one point light; the environment lighting is not raised.

### Chapters and progress

`ChapterDatabase` and `ChapterDefinition` describe the available chapters and their scenes. `GameProgress` stores the furthest chapter/scene and the unlocked chapter list. The main menu uses this to enable **Continue** and the chapter select screen.

Progress is separate from settings, so changing graphics options never affects story progress.

### Scene flow and transitions

`SceneFlow` is the menu-facing entry point for scene changes. `SceneTransitionManager` presents the black loading screen, loading indicator, and chapter introduction cards. Gameplay transitions such as entering the car, reaching the store, and returning to the city continue to use `SceneManager` directly.

### Music and audio

`MusicManager` is persistent and scene-independent. It reads `Resources/Paralysis/MusicDatabase`, plays the cue bound to each scene, and supports one-shot story cues such as the lights going out, the store, and the ending. Missing cues are skipped with a warning instead of breaking the scene.

### Save data

`SaveIO` writes two JSON files under `Application.persistentDataPath/Paralysis`:

- `settings.json` — mouse sensitivity, frame rate, VSync, resolution, and motion blur.
- `progress.json` — chapter progress and unlocked chapters.

The settings file has a version field. Version 1 files receive the new graphics defaults instead of silently turning the new options off.

---

## Project architecture

```
Assets/
  code/                     Gameplay scripts (story, driving, interaction, UI runtime)
  Scripts/
    Chapters/               Chapter database and definitions
    Dialogue/               Dialogue database and configuration
    Ending/                 End screen controller and view
    MainMenu/               Main menu, chapter select, lightning flash
    Pause/                  Pause manager and config
    Save/                   Progress, session cleanup, save IO
    SceneFlow/              Loading screen, chapter intros, transitions
    Settings/               GameSettings, settings UI, resolution guard, motion blur
    UI/                     Shared UI widgets, views, audio, animation
  Editor/                   Editor-only tools (sync, destination builder, smoke runner)
  Resources/Paralysis/      Authored data assets and shared UI prefabs
  Dialogue/                 Conversation assets
  Scenes/                   MainMenu, Home, City, CarDriving, Store, End
  Materials/                Shared materials
  PREFAB/                   Shared prefabs
  ...                       Third-party art, audio, and environment packs
```

### Runtime flow

```
MainMenu
   └─ SceneFlow → Home
        └─ City
             └─ CarDriving
                  └─ Store
                       └─ End
```

The persistent managers (`StoryDirector`, `DialogueSystem`, `ObjectiveManager`, `ShoppingCart`, `MusicManager`, `PauseManager`, and the settings services) are created on demand and survive scene changes. Duplicate managers deactivate and destroy themselves so a scene load cannot create two audio listeners, two players, or two event systems.

### Environment synchronization

`City` is the source of truth for the shared city environment. `DrivingSceneSyncTool` copies the city's environment into `CarDriving` while preserving the driving-only roots:

- `CarDriveSystems`
- `Store Destination`
- `StoryManagers`
- `FirstPersonController`
- `Free Racing Car`

The tool copies prefab instances, overrides, materials, RenderSettings, fog, skybox, and ambient lighting. It does not touch the driving controller, camera, destination, or player.

Editor menu:

```
Paralysis > Scenes > Driving Sync > Dry Run (report only)
Paralysis > Scenes > Driving Sync > Apply City -> CarDriving
```

The dry run writes `Logs/driving_sync.txt` and reports any environment differences. The apply path writes the same report with `Mode: APPLY` and `Result: saved`.

### Settings architecture

- `GameSettings` — static settings state, persistence, and application.
- `SettingsConfig` — Inspector-editable ranges, defaults, and options (`Assets/Resources/Paralysis/SettingsConfig.asset`).
- `SettingsPanel` — builds the runtime rows inside the existing MainMenu and PauseMenu panels.
- `ResolutionGuard` — persistent confirmation modal with KEEP/REVERT and a 15-second automatic revert.
- `MotionBlurController` — persistent global URP `Volume` and camera post-processing state.

The MainMenu and PauseMenu use the same settings panel logic but different authored layouts. The MainMenu panel uses a compact row layout; the stretched PauseMenu panel uses the normal spacing.

---

## Settings

### Mouse sensitivity

- Range: `0.1` to `10.0`
- Default: `2.0`
- Applied immediately to the existing first-person controller and the driving camera.

### Frame rate

- Options: `60`, `120`, `240` FPS
- Default: `60` FPS
- When VSync is off, the selected value is applied through `Application.targetFrameRate`.

### VSync

- Default: off, preserving the frame-rate cap behavior.
- On: `QualitySettings.vSyncCount = 1` and `Application.targetFrameRate = -1` (the display refresh rate becomes the limit).
- Off: `QualitySettings.vSyncCount = 0` and the selected frame-rate cap is restored.

VSync and the FPS cap are intentionally mutually exclusive so the two settings cannot fight each other.

### Resolution

- Options come from `Screen.resolutions`, deduplicated by width and height.
- Range: `1024 × 600` to `3840 × 2160`.
- The current window/display mode is included as a fallback when the platform reports no modes.
- Selecting a mode applies it immediately, then opens a confirmation prompt.
- The prompt gives the player 15 seconds to confirm. `KEEP` (or `Enter`) saves the mode; `REVERT` (or `Esc`) restores the previous mode.
- A resolution is only written to disk after confirmation.
- A saved mode that the current display no longer reports is not applied. The game falls back to 1080p when supported, otherwise to the largest supported mode that does not exceed the saved size.

### Motion blur

- Default: on.
- Uses URP's built-in `MotionBlur` volume override.
- Mode: `CameraAndObjects`; quality: `Medium`; intensity: `0.35`; clamp: `0.075`.
- The effect is applied through a persistent global volume so it survives scene changes and works with the runtime-created driving camera.
- While enabled, the pipeline's sample global profile is temporarily removed so enabling post-processing does not add tonemapping or a vignette to the game's look.
- The main menu camera is left untouched to preserve the title screen's authored look.
- Turning the option off restores each camera's original `renderPostProcessing` value.

### Settings file

Settings are saved to:

```
%USERPROFILE%/AppData/LocalLow/<company>/<product>/Paralysis/settings.json
```

On Windows, the exact path is shown by `Application.persistentDataPath`. Deleting `settings.json` resets all graphics options to their defaults on the next launch.

---

## Setup

### Requirements

-Strong PC/Laptop that can handle the game easily


### Open the project
**Play through Itch.io website I wrote underneath or**
1. Clone the repository.
2. Open the folder with Unity Hub using Unity `6000.7.0a6`.
3. Let Unity import the packages and assets. The first import can take several minutes because the project includes large environment and character packs.
4. Open `Assets/Scenes/MainMenu.unity`.
5. Press Play.

### Recommended player settings

- Fullscreen mode: your preference; resolution changes preserve the current fullscreen mode.
- VSync: off by default so the FPS cap is predictable.
- Resolution: 1920 × 1080 is a safe default for most displays.

---

## Credits

**PARALYSIS** is a solo narrative horror demo. Game, story, design, and direction by me, Serali. With some inspiration from his friends

Built with:

- Unity 6.7 Alpha and the Universal Render Pipeline
- TextMesh Pro
- Unity Input System package (gameplay input uses the legacy Input Manager axes)

Third-party art, environment, character, music, and sound packs are included under their original licenses. See the corresponding folders for the pack readmes and license files, including:

- ARCADE - FREE Racing Car
- Modular First Person Controller
- Gridness Studios Grocery Store Pack Lite
- LowlyPoly
- Layer Lab 3D Props-AdorableFoods
- Mnostva Art Food
- Halloween Pumpkin
- Modular Lowpoly Streets Free
- Real Stars Skybox
- vrbn studios city buildings
- BxB Studio
- AN Interactive Physical Door Pack
- Mono Limbo
- DearMousey
- npc_casual_set_00
- Sat Productions
- Epic Adventure Orchestral Background Music
- Music Atmosphere Horror
- Sounds

Special thanks to everyone who helped me with the game, playtested the demo and reported issues.
Those are: Pinyuan Chen, who tested my game, and gave me a review out of 10 (he said 0 cuz it scared him so much); and Paya, who helped me with the idea for this game. 
I did use AI, however, it was only for debugging as this game had so many bugs i couldn't count them all.

Thank you for reading this README.
You will be able to play the game from the https://ciklole.itch.io/paralysis
