# Spec written by the developer (Serali / ciklole1)

Sent to the AI agent on 2026-10-26 01:07 UTC, 38173 characters, in the chat session.
Reproduced here verbatim from the session history.
---

# PROJECT: PARALYSIS — MAIN MENU AND GAME UI SYSTEMS

## 1. YOUR TASK

I want you to implement a complete, polished, customizable UI system for my Unity horror game, Paralysis.

This task includes:

1. Synchronizing the driving scene's environment with the City scene.
2. Creating a new, separate Main Menu scene.
3. Implementing New Game, Continue, Chapter Selection, Settings, and Quit Game.
4. Implementing a centralized pause system.
5. Creating an animated pause menu and pause-menu settings screen.
6. Implementing persistent player settings.
7. Integrating mouse sensitivity with my existing first-person controller.
8. Implementing configurable frame-rate limits.
9. Creating smooth scene-transition loading screens.
10. Displaying customizable chapter-introduction titles during chapter transitions.
11. Organizing scripts and other assets into logical folders.
12. Making the entire UI customizable through the Unity Inspector wherever practical.

IMPORTANT: Before modifying anything, inspect the existing Unity project, relevant scenes, scripts, prefabs, UI systems, and scene-transition logic.

Understand how the current project works before implementing these features.

Reuse existing functionality wherever possible. Do not create duplicate systems unnecessarily, and do not replace working systems simply because you prefer a different implementation.

This is a real game project with existing gameplay systems. Preserve compatibility with those systems.

---

# 2. CRITICAL SAFEGUARDS — DO NOT BREAK EXISTING CONTENT

## 2.1. The remodeled store

I have remodeled the store in the City scene.

**DO NOT MODIFY THE REMODELED STORE.**

Do not rebuild it, redesign it, move its objects, replace its materials, change its lighting, alter its layout, or modify its existing functionality.

Treat the remodeled store as protected content.

Do not overwrite the City scene with an older version or replace it with a duplicate scene.

The purpose of this task is to implement the requested systems while preserving my existing work.

## 2.2. Existing player controllers

My project uses two different player controllers in two separate scenes.

- **City scene:** The player controls a person.
- **Driving scene:** The player controls the car.

Both systems already work.

Do not merge them, replace them, or create a universal controller that changes how either scene works.

Keep the existing scene-specific controllers intact.

Only modify their behavior if a requested feature genuinely requires integration, and make the smallest compatible change possible.

## 2.3. Existing first-person controller

My existing first-person controller is located here:

`Assets/ModularFirstPersonController/FirstPersonController`

It already handles mouse locking and mouse sensitivity.

Inspect its actual scripts and configuration before making any changes.

Do not create a second first-person controller, a second mouse-look system, or an unrelated sensitivity implementation.

Integrate the new UI with the existing controller.

## 2.4. Existing loading-screen implementation

My `Teleport.cs` script already implements the type of loading-screen behavior I want.

Inspect `Teleport.cs` and any relevant teleportation scripts to understand how the loading screen currently works.

Reuse that implementation or its existing components where practical.

The new scene-transition system should follow the same loading-screen behavior and visual style.

Do not unnecessarily rewrite my teleportation systems.

Do not break existing teleports, scene transitions, player scaling, or other teleportation functionality.

## 2.5. General preservation requirements

Before editing files:

- Inspect the existing project structure.
- Identify the current scenes and their scene names.
- Identify existing dialogue, interaction, lighting, and scene-transition systems.
- Check whether any existing UI components can be reused.
- Check for scripts or prefabs that might conflict with the new systems.
- Identify how the current game determines which scene is loaded.
- Identify how player input is currently handled.

Do not delete existing scripts or assets simply because they are not needed for the new UI.

Do not make unrelated changes to gameplay, story progression, lighting, dialogue, interactions, or other systems.

If a requested feature conflicts with an existing implementation, inspect the conflict and choose the least disruptive solution.

If a critical implementation detail cannot be determined safely, explain the issue instead of making a destructive assumption.

---

# 3. CITY SCENE AND DRIVING SCENE SYNCHRONIZATION

The City scene and the driving scene are separate Unity scenes.

The driving scene already works, and the car controller is already implemented.

However, I want changes made to the City scene to be reflected in the driving scene's environment.

For example, if I change an appropriate building, road, streetlight, tree, or other environmental object in the City scene, the corresponding environment in the driving scene should reflect that change.

## Requirements

1. Inspect how the City scene and driving scene are currently constructed.
2. Determine which objects are shared between the environments and which objects are unique to the driving scene.
3. Identify the safest way to synchronize the environments without affecting either controller.
4. Implement a workflow that minimizes or eliminates manual duplication of environmental changes.
5. Preserve scene-specific objects, including the car, car controller, player controller, cameras, triggers, and driving-specific gameplay elements.
6. Preserve the remodeled store and all its existing content.
7. Avoid creating duplicate objects when updating the driving environment.
8. Do not overwrite the entire driving scene with the City scene.
9. Do not remove or replace the driving scene's unique objects.
10. Verify that the existing driving gameplay still works after synchronization.

The objective is to keep the two environments visually consistent while allowing each scene to retain its own gameplay systems.

If the current scene structure requires a shared prefab, nested prefab, or another controlled synchronization method, inspect the project first and implement the most appropriate solution.

Do not assume that every object in the City scene should also appear in the driving scene. Preserve the distinction between shared environment elements and scene-specific content.

Document the synchronization method so I understand how future City scene changes will be reflected in the driving scene.

---

# 4. MAIN MENU SCENE

Create a new Unity scene specifically for the main menu.

Do not turn an existing gameplay scene into the main menu.

Use a clear scene name, such as:

`MainMenu`

Register the scene in Build Settings or the project's equivalent scene-inclusion configuration, as appropriate.

The main menu must work when the game launches.

## 4.1. Main menu appearance

The title should display:

PARALYSIS

Position the title near the top-center of the screen.

Use a normal placeholder font for this title. I will customize the title's appearance later.

Do not use the pixelated electronic font for the main title.

The title must not have a visible rectangular background or button panel.

I want a clean, atmospheric menu.

The title's following properties must be configurable:

- Text.
- Font.
- Font size.
- Position.
- Alignment.
- Color.
- Letter spacing, if supported.
- Other appropriate text styling properties.

Do not permanently hardcode the title's appearance into a script.

## 4.2. Main menu buttons

Create the following buttons:

- NEW GAME
- CONTINUE
- CHAPTERS
- SETTINGS
- QUIT GAME

Use the existing pixelated electronic font already imported into my Unity project.

Find and reuse the appropriate font asset rather than downloading a replacement.

The buttons must have text-only visuals.

Do not draw visible rectangles, boxes, panels, or backgrounds underneath the button text.

The actual clickable hitboxes should be invisible.

The invisible hitboxes must be large enough to click comfortably and must not overlap other buttons unintentionally.

Make sure the buttons remain usable at different screen resolutions.

## 4.3. Hover animation

When the cursor moves over a button:

1. The button text should move slightly upward.
2. The text should become bold or use an appropriate bold font asset or supported text-style alternative.
3. The transition should be smooth rather than instantaneous.
4. The button should return smoothly to its normal position and appearance when the cursor leaves.

Make the following properties configurable:

- Vertical movement distance.
- Animation duration.
- Easing or interpolation behavior.
- Normal font size.
- Hover font size, if used.
- Normal text appearance.
- Hover text appearance.

Do not make the text jump abruptly between states.

The hover animation must not cause buttons to move outside their intended hitboxes or become difficult to select.

## 4.4. Hover and click sounds

Add configurable hover and click sounds.

Use suitable electronic or atmospheric UI sound effects.

You may find appropriate sounds online if necessary, but use legitimate sources and comply with the relevant usage and licensing requirements.

Prefer existing project audio assets if appropriate.

Requirements:

- Play the hover sound when the pointer enters a button.
- Do not repeatedly play the hover sound every frame.
- Play the click sound when a button is activated.
- Avoid stacking multiple copies of the same sound accidentally.
- Allow sounds to be replaced or disabled through the Inspector.
- Do not make UI sounds excessively loud.

If no appropriate sound asset is available, use a configurable placeholder rather than introducing an unreliable external dependency.

## 4.5. Main menu layout

Make the layout adaptable to different screen resolutions and aspect ratios.

Keep the title and buttons properly positioned.

Avoid unnecessary UI panels and visible button backgrounds.

Make the menu easy to navigate and maintain.

---

# 5. NEW GAME, CONTINUE, AND SAVE PROGRESS

## 5.1. NEW GAME

When the player selects NEW GAME:

- Begin a new game session.
- Initialize the game's new-game state using the project's actual scene structure.
- Load the appropriate starting scene.
- Initialize the appropriate chapter or starting sequence.
- Avoid accidentally carrying over chapter progress from a previous game.
- Preserve the player's saved settings, such as mouse sensitivity and frame-rate preference.

Starting a new game must not erase the player's settings.

Inspect the existing project to determine the appropriate starting scene instead of guessing.

Do not modify existing story events unnecessarily.

## 5.2. CONTINUE

When the player selects CONTINUE:

- Load the player's previously saved game progress.
- Restore the appropriate saved chapter and game state, using the project's actual save architecture.
- Load the correct scene.
- Do not accidentally start a new game.

If no saved game exists, make CONTINUE unavailable or clearly communicate that no saved game exists.

Do not silently load an unrelated scene.

Inspect the existing project for save/load functionality before implementing this feature.

If the project does not yet have a suitable save system, implement the minimum reliable save/load structure needed for this feature without inventing complicated checkpoint or death systems.

Keep the implementation modular so the save system can be expanded later.

## 5.3. Save progress versus settings

Keep game progress and player preferences logically separate.

Player settings must persist independently of starting a new game.

Do not erase the player's settings when resetting story progression.

---

# 6. CHAPTER SELECTION

Create a dedicated chapter-selection interface accessible through CHAPTERS on the main menu.

The chapter-selection interface must allow me to configure the chapters through the Inspector.

Do not permanently hardcode the chapter list into the UI.

## 6.1. Chapter configuration

Each chapter entry should support configurable properties such as:

- Chapter name.
- Chapter number or identifier.
- Short description, if desired.
- Scene to load.
- Whether the chapter is available.
- Display order.
- Optional chapter-introduction title.
- Optional transition settings.

Use an appropriate data-driven configuration, such as serializable chapter entries or ScriptableObjects, depending on the existing project architecture.

Avoid creating unnecessary complexity.

## 6.2. Development mode

For now, this is a development build.

**Every configured chapter must be selectable for testing, even if the player has not completed the previous chapters.**

I need to test individual chapters without completing the entire game first.

Create a clearly named configuration option for chapter availability behavior.

The development configuration should allow all chapters to be selected.

Do not implement progression restrictions in the initial development configuration.

## 6.3. Future player mode

I will later ask you to change the project to player mode when I am preparing the game for publication.

At that point, chapter availability should be based on game progression.

However, do not switch to player mode now.

Instead, structure the chapter-selection system so that the availability behavior can be changed through a clear configuration option or a small, controlled modification.

The future player mode should support:

- Unlocking chapters as the player progresses.
- Preventing access to chapters that have not been unlocked.
- Preserving chapter-unlock progress between sessions.
- Keeping any intentionally available starting chapter accessible.

Do not implement unrelated death, checkpoint, or respawn systems as part of this task.

## 6.4. Selecting a chapter

When a chapter is selected:

1. Identify the correct scene associated with that chapter.
2. Begin the appropriate transition.
3. Display the loading screen.
4. Display the configured chapter-introduction title, if enabled.
5. Load the selected chapter.
6. Initialize the appropriate gameplay state without accidentally starting an unrelated chapter.

Add a BACK button to return to the main menu.

Chapter entries must be easy for me to add, remove, rename, reorder, and reconfigure.

---

# 7. SETTINGS MENU

Create a settings interface accessible from the main menu.

For now, the settings background should be a plain, empty background.

Do not spend time designing a complicated background. I will customize it later.

However, the settings controls themselves should be properly implemented.

The settings screen must use the same electronic pixelated font as the other menu controls.

## 7.1. Mouse sensitivity

Create a configurable mouse-sensitivity control.

Prefer a slider with a displayed value.

Requirements:

- Integrate with the existing first-person controller.
- Use the controller located in `Assets/ModularFirstPersonController/FirstPersonController`.
- Inspect its actual sensitivity settings and implementation.
- Update the existing sensitivity parameter rather than creating a second mouse-look system.
- Apply changes immediately when appropriate.
- Restore the saved sensitivity when the game launches.
- Save changes between sessions.
- Make the minimum, maximum, default value, and displayed precision configurable.

Do not assume the controller uses a particular variable name without inspecting its scripts.

If the existing controller requires a specific initialization method or API to update sensitivity safely, use that method.

Do not break the controller's existing mouse-locking behavior.

## 7.2. Frame-rate limit

Create a selectable frame-rate setting.

Provide these options:

- 60 FPS.
- 120 FPS.
- 240 FPS.

Use 60 FPS as the default.

The selected option should act as a frame-rate cap, not as a promise that the hardware will always reach that frame rate.

Implement the appropriate Unity frame-rate configuration.

Account for the platform and Unity's frame-pacing behavior where necessary.

Do not confuse the frame-rate target with the monitor's refresh rate.

Apply the selected setting appropriately and save the preference between sessions.

Restore the saved selection when the game launches.

Make the available options and default value configurable.

Do not add unnecessary graphics settings that I have not requested.

## 7.3. Persistence

Save the player's settings between game sessions.

At minimum, persist:

- Mouse sensitivity.
- Frame-rate preference.

Changing chapters, starting a new game, returning to the main menu, or restarting the application must not unnecessarily reset these preferences.

Use an appropriate persistence mechanism for the project.

Keep the settings implementation modular so that additional preferences can be added later.

## 7.4. Customization

Make the settings labels, fonts, font sizes, layout, slider ranges, displayed values, and other appropriate UI properties configurable through the Inspector.

Avoid hardcoding presentation details into gameplay scripts.

---

# 8. CENTRALIZED PAUSE SYSTEM

There is currently no centralized pause system.

Create one that integrates with the existing gameplay architecture.

Do not create a completely separate pause implementation for every scene.

Use a centralized pause-management component with clearly defined responsibilities.

The system must support the relevant gameplay scenes without merging their player controllers.

## 8.1. Opening the pause menu

When the player presses ESC during gameplay:

- Open the pause menu.
- Stop gameplay.
- Unlock and display the mouse cursor.
- Prevent the player from moving.
- Prevent the player from jumping.
- Prevent mouse-look camera rotation.
- Prevent object interactions.
- Prevent other gameplay actions from responding to input.

The existing dialogue, interaction, lighting, and player systems must not continue responding to gameplay input while paused.

Inspect how these systems currently handle input before integrating them.

Do not assume that setting Unity's `Time.timeScale` to zero automatically stops every system.

Account for input processing, coroutines, unscaled-time updates, and any relevant systems that could continue running while the game is paused.

The UI must remain responsive while gameplay is paused.

## 8.2. Pause menu layout

The pause menu must divide the screen visually into two areas.

### LEFT SIDE

The left side should be completely black.

It contains the pause-menu buttons:

- CONTINUE
- SETTINGS
- REPLAY CHAPTER
- MAIN MENU

Use the pixelated electronic font.

Do not place visible rectangular backgrounds underneath the buttons.

### RIGHT SIDE

The right side should display the current gameplay scene.

The game should remain visible, but:

- The image should be darker than normal.
- The image should be partially obscured by a configurable translucent overlay.
- The player must not be able to interact with the gameplay scene.

The left side must remain completely black and visually distinct from the right side.

Use a layout that adapts to different screen resolutions.

Make the dividing position configurable.

Make the right-side darkening amount and overlay transparency configurable.

## 8.3. Pause-menu opening animation

When ESC is pressed, play a smooth opening animation.

I want the pause screen to feel polished and atmospheric.

For example, the black left panel could slide into position while the right-side darkening overlay fades in.

You may choose an appropriate animation that fits the game's horror atmosphere.

Requirements:

- Smooth transitions.
- No abrupt visual jumps.
- No unnecessary stuttering.
- Configurable animation duration.
- Configurable easing.
- Correct behavior when the menu is opened repeatedly.
- No duplicate UI objects being created every time ESC is pressed.

Gameplay should pause reliably without allowing the player to continue moving or interacting during the transition.

The UI animation itself must continue playing while gameplay is paused.

Do not allow the opening animation to accidentally unpause the game.

## 8.4. CONTINUE

When CONTINUE is selected:

- Close the pause menu.
- Restore gameplay.
- Restore the appropriate player input.
- Restore the correct cursor-locking behavior.
- Restore the game's previous time scale rather than assuming that every scene always uses the same value.

The closing animation should be smooth and configurable.

The cursor should return to the state expected by the current gameplay scene.

For example, the first-person controller should resume its normal mouse-locking behavior.

Do not leave the mouse unlocked during gameplay unless the existing game explicitly requires it.

## 8.5. REPLAY CHAPTER

When REPLAY CHAPTER is selected:

- Identify the currently active chapter.
- Reload the appropriate chapter starting scene or configured entry point.
- Use the normal scene-transition system.
- Show the loading screen when appropriate.
- Initialize the chapter correctly.

Do not implement checkpoint-based respawning, death handling, or a new checkpoint system as part of this feature.

Keep chapter restarting separate from future death and respawn functionality.

## 8.6. MAIN MENU

When MAIN MENU is selected:

- Return the player to the new MainMenu scene.
- Use the normal transition and loading-screen system.
- Prevent gameplay from continuing in the background.
- Restore the appropriate game state when the menu loads.
- Do not accidentally erase saved progress or player settings.

If the project requires a confirmation before leaving gameplay, make that behavior configurable.

## 8.7. ESC behavior

If the pause menu is open and the player selects SETTINGS, open the pause-menu settings screen.

If the settings screen is open and the player presses ESC, return to the pause menu.

**Pressing ESC inside settings must not resume gameplay.**

The settings screen must include a BACK button.

Pressing BACK should close the settings screen and return to the normal pause menu.

Returning from settings must not unpause the game or lose the player's current settings.

Prevent ESC input from accidentally triggering multiple transitions in the same frame.

---

# 9. PAUSE-MENU SETTINGS

The pause-menu settings screen should reuse the same settings implementation as the main-menu settings screen.

Do not create a separate sensitivity system or frame-rate system just for the pause menu.

When SETTINGS is selected from the pause menu:

1. Keep the game paused.
2. Keep the cursor visible and unlocked.
3. Hide the pause-menu buttons.
4. Display the settings interface on the left side.
5. Keep the right-side gameplay view darkened and visible.
6. Allow the player to modify settings.
7. Apply changes appropriately.
8. Save the settings as required.
9. Display a BACK button.

When BACK is selected:

- Hide the settings interface.
- Restore the pause-menu buttons.
- Keep gameplay paused.
- Preserve the current UI layout and correct cursor state.

The settings screen must not accidentally remove the darkened gameplay view or make the left side transparent.

Use the same reusable settings component or data model for both settings interfaces.

Allow the main-menu settings layout and pause-menu settings layout to be configured independently where necessary, while sharing their underlying functionality.

---

# 10. LOADING SCREEN AND SCENE TRANSITIONS

My existing `Teleport.cs` implementation already contains the kind of loading-screen behavior I want.

Inspect it and reuse the relevant implementation.

I want the new scene-transition system to provide consistent loading behavior across the game.

## 10.1. Loading indicator

Use the same general loading-indicator style as the existing teleportation system.

For example:

LOADING

LOADING.

LOADING..

LOADING...

Repeat the animation smoothly.

Requirements:

- Smooth dot animation.
- Configurable animation speed.
- No unnecessary object creation.
- No visible stuttering caused by the animation.
- Configurable text, font, size, and position.
- Compatibility with the existing font assets.
- Correct behavior during scene loading.
- No duplicate loading screens during the same transition.

The indicator must remain visible while the relevant transition is in progress.

Do not unnecessarily modify the existing teleportation code to achieve this.

## 10.2. Scene-transition integration

Use the loading screen for scene transitions, including:

- Starting a new game.
- Continuing a saved game.
- Selecting a chapter.
- Returning to the main menu.
- Restarting a chapter.

Ensure that the correct target scene is loaded.

Do not allow the player to interact with the destination scene before the transition has finished and gameplay has been initialized.

Avoid leaving the loading screen permanently visible if a transition fails.

Use appropriate error handling and diagnostic messages.

Do not introduce artificial long delays merely to display the loading screen.

## 10.3. Chapter-introduction screen

I also want a chapter-introduction screen during chapter transitions.

When entering a chapter, display a black screen with the chapter number and title in large text.

For example:

CHAPTER 1

THE BEGINNING

The actual chapter number and title must come from configurable chapter data.

These are only example values.

The screen should display:

- A completely black background.
- A large chapter number or identifier.
- The chapter title.
- An optional subtitle if configured.

Use an appropriate transition from the loading screen to the chapter title and then into the actual game.

The text should be readable, properly centered or positioned according to configurable layout settings, and consistent with the horror atmosphere.

## 10.4. Chapter-introduction customization

I must be able to customize:

- Chapter number.
- Chapter title.
- Subtitle.
- Font.
- Font size.
- Text alignment.
- Text position.
- Text color.
- Fade-in duration.
- Fade-out duration.
- Display duration.
- Whether the chapter-introduction screen is enabled for each chapter.

Do not permanently hardcode chapter titles into the transition script.

The chapter-introduction system should obtain its information from the selected chapter's configuration.

The system must work when I add or rename chapters in the Inspector.

---

# 11. UI CUSTOMIZATION REQUIREMENTS

Customization is a major requirement for this project.

I want to be able to change the UI's appearance without repeatedly asking you to modify the C# scripts.

Where practical, expose configuration through the Unity Inspector.

This includes:

### Text

- Button labels.
- Menu titles.
- Chapter titles.
- Chapter numbers.
- Settings labels.
- Loading text.
- Font assets.
- Font sizes.
- Text colors.
- Alignment.
- Letter spacing, where supported.

### Layout

- Button positions.
- Button spacing.
- Title position.
- Settings layout.
- Pause-menu division.
- Panel dimensions.
- Margins and padding.
- UI scaling.
- Relevant responsive-layout settings.

### Animations

- Hover movement distance.
- Hover animation duration.
- Pause-menu opening animation.
- Pause-menu closing animation.
- Fade durations.
- Easing.
- Chapter-title transitions.
- Loading-indicator speed.

### Audio

- Hover sound.
- Click sound.
- Optional transition sound.
- Sound volume.
- Ability to disable optional sounds.

### Gameplay preferences

- Default mouse sensitivity.
- Minimum and maximum mouse sensitivity.
- Default frame-rate limit.
- Available frame-rate options.

### Chapter configuration

- Chapter names.
- Chapter identifiers.
- Target scenes.
- Chapter ordering.
- Availability.
- Chapter-introduction text.
- Optional chapter-introduction settings.

Do not expose meaningless implementation details just to create more Inspector fields.

Prioritize settings that are genuinely useful to me as the developer.

Use appropriate serialized fields, reusable components, ScriptableObjects, or other maintainable approaches where they fit the existing architecture.

---

# 12. SCRIPT AND ASSET ORGANIZATION

I want the project to remain organized as the number of systems increases.

**Do not put all the new functionality into one enormous C# script.**

Organize scripts into folders according to their responsibilities.

For example, use a structure similar to the following, adapting it to the existing project where necessary:

Assets/
    Scripts/
        MainMenu/
            MainMenuController.cs
            MainMenuButton.cs

        ChapterSelection/
            ChapterSelectionController.cs
            ChapterDefinition.cs
            ChapterAvailabilityManager.cs

        Settings/
            SettingsManager.cs
            SettingsMenuController.cs
            PlayerSettingsData.cs

        PauseMenu/
            PauseManager.cs
            PauseMenuController.cs
            PauseMenuAnimation.cs

        LoadingScreen/
            LoadingScreenController.cs
            LoadingIndicator.cs
            ChapterIntroductionController.cs

        SceneManagement/
            SceneTransitionManager.cs

        SaveSystem/
            SaveManager.cs
            GameProgressData.cs

This is a proposed structure, not an instruction to create every file regardless of need.

Inspect the existing folder structure first.

Reuse existing scripts when appropriate, and avoid creating duplicate classes that perform the same responsibilities.

For example:

- The pause manager should coordinate pausing and resuming.
- The pause-menu controller should handle the pause menu's UI.
- The settings manager should handle settings values and persistence.
- The scene-transition manager should coordinate scene loading.
- The loading-screen controller should handle loading-screen presentation.
- The chapter-selection controller should handle chapter-selection UI.
- Chapter data should be separated from the visual UI when appropriate.

If an existing StoryManager or another system already has an appropriate folder, preserve its organization.

For story-related scripts, use a clearly named folder such as `StoryManager` if that matches the project's existing naming conventions.

Do not move existing scripts unnecessarily, especially if doing so could break references or other systems.

Organize newly created assets into appropriate folders as well, including prefabs, UI resources, and other supporting assets.

Use clear, consistent names.

Avoid duplicate managers, ambiguous class names, and circular dependencies.

---

# 13. IMPLEMENTATION QUALITY AND RELIABILITY

The new systems must be reliable and maintainable.

## 13.1. Existing functionality

Do not break:

- The existing City scene.
- The remodeled store.
- The driving scene.
- The car controller.
- The existing first-person controller.
- Existing object interactions.
- Existing dialogue systems.
- Existing lighting systems.
- Existing teleportation.
- Existing story progression.
- Existing scene transitions.

Only make necessary changes to support the requested features.

## 13.2. Pause-state reliability

Ensure that:

- The game cannot continue responding to gameplay input while paused.
- The cursor is unlocked when appropriate.
- The cursor returns to the correct state when gameplay resumes.
- The pause menu remains interactive while the game is paused.
- The game does not become permanently paused after a scene transition.
- The game does not resume accidentally when navigating between pause-menu screens.
- Repeated ESC presses do not create duplicate UI objects or inconsistent states.
- Time-scale changes are restored appropriately.

If a system relies on unscaled time, coroutines, or input processing outside the normal gameplay loop, inspect whether it needs explicit pause handling.

## 13.3. Scene-transition reliability

Ensure that:

- Only the intended destination scene is loaded.
- The correct chapter is initialized.
- Loading screens do not remain stuck indefinitely.
- Repeated button presses cannot start conflicting transitions.
- Returning to the main menu does not accidentally erase saved progress.
- The correct cursor state is restored after loading.
- Chapter-introduction text corresponds to the chapter being entered.

## 13.4. Performance

Avoid unnecessary performance overhead.

In particular:

- Do not create UI objects repeatedly during animations.
- Do not run unnecessary Update loops.
- Do not perform repeated scene searches every frame.
- Avoid expensive operations in frequently executed code.
- Avoid introducing unnecessary third-party dependencies.
- Keep animations efficient.
- Do not introduce avoidable frame-rate drops.

Do not promise that every loading transition will be completely free of stuttering, since actual scene-loading performance depends on the project's content and hardware. Instead, implement the transitions efficiently and avoid introducing additional stuttering.

---

# 14. IMPLEMENTATION WORKFLOW

Please approach the task systematically.

## Phase 1: Inspect the project

Before writing code:

1. Inspect the scene structure.
2. Locate the City scene and driving scene.
3. Inspect how their environments are constructed.
4. Inspect `Teleport.cs`.
5. Inspect relevant teleportation scripts.
6. Inspect the first-person controller in `Assets/ModularFirstPersonController/FirstPersonController`.
7. Inspect existing dialogue and interaction systems.
8. Inspect existing story-management systems.
9. Inspect the existing save system, if any.
10. Inspect available fonts and UI assets.
11. Check scene inclusion and build configuration.
12. Identify possible conflicts with the requested systems.

Do not start by creating dozens of scripts without understanding the existing project.

## Phase 2: Plan the implementation

Determine:

- Which existing systems can be reused.
- Which new systems are necessary.
- How the systems will communicate.
- How scene transitions will work.
- How the pause manager will coordinate with gameplay.
- How chapter configuration will work.
- How settings will persist.
- How the City and driving environments will remain synchronized.

Keep the architecture as simple as practical while preserving the required functionality.

## Phase 3: Implement the systems

Implement the features in a logical order.

A suitable order is:

1. City/driving environment synchronization.
2. Main menu scene and UI.
3. Chapter configuration and selection.
4. Settings and persistence.
5. Centralized pause management.
6. Pause menu and animations.
7. Shared settings integration.
8. Loading-screen integration.
9. Chapter-introduction screens.
10. Script and asset organization.
11. Final integration and testing.

Adapt this order if the existing project architecture requires a different sequence.

## Phase 4: Verify the implementation

After implementation:

- Check for compilation errors.
- Resolve errors introduced by the changes.
- Verify scene references.
- Verify serialized references and prefab connections.
- Check for duplicate managers.
- Verify the main menu can launch the game.
- Verify chapter selection loads the correct scenes.
- Verify Continue behaves correctly when a save exists and when no save exists.
- Verify settings persist after restarting the game.
- Verify mouse sensitivity modifies the existing controller.
- Verify frame-rate selections configure the intended cap.
- Verify the pause menu prevents gameplay input.
- Verify ESC inside settings returns to the pause menu.
- Verify pause-menu animations work while the game is paused.
- Verify the loading screen works with the existing transition architecture.
- Verify chapter-introduction text is configurable.
- Verify the driving scene still works.
- Verify the remodeled store has not been altered.

If you have access to Unity's editor or automated testing tools, use them to validate the implementation rather than relying exclusively on static code inspection.

Do not claim that a feature has been tested in Unity if you have only inspected the source code.

## Phase 5: Report the results

When finished, provide a concise but useful summary of:

1. What you implemented.
2. Which scenes were created or modified.
3. Which scripts were created or modified.
4. Where the scripts are located.
5. How to customize the main menu.
6. How to configure chapters.
7. How to configure settings.
8. How to configure the pause menu.
9. How the loading screen integrates with the existing teleportation system.
10. How City-to-driving environment synchronization works.
11. What you tested.
12. Any remaining limitations or unfinished work.

Clearly distinguish completed work from anything that still requires manual setup or testing.

---

# 15. FINAL REQUIREMENTS

The finished result should feel like a cohesive game UI rather than a collection of unrelated menus.

The main menu should be clean and customizable.

The button animations should feel smooth.

The pixelated electronic font should be used consistently for the requested UI elements, except for the main `PARALYSIS` title, which should use a normal placeholder font.

The pause menu should have the split-screen appearance described above, with a black left side and a darkened, partially transparent view of the paused game on the right.

Settings must work consistently from both the main menu and the pause menu.

Scene transitions should reuse the established loading-screen behavior from my existing teleportation system.

Chapter transitions should support large, configurable chapter titles on a black background.

The code must be organized into appropriate folders and separate components.

Most importantly, **preserve my existing work and avoid breaking functional gameplay systems.**

Implement the requested features without adding unrelated gameplay mechanics or redesigning content I have already finished.

If you discover that an existing system prevents a requirement from being implemented safely, explain the conflict and propose the smallest appropriate change instead of silently replacing the system.