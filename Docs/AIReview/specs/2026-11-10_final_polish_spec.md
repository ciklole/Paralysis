# Spec written by the developer (Serali / ciklole1)

Sent to the AI agent on 2026-11-10 23:24 UTC, 29746 characters, in the chat session.
Reproduced here verbatim from the session history.
---

# FINAL DEVELOPMENT TASK — PARALYSIS
## The Final Polish, Scene Synchronization, Graphics Settings, Documentation, and Git Push

**THIS IS THE FINAL DEVELOPMENT TASK FOR PARALYSIS.**

WE ARE OFFICIALLY FINISHED MAKING THE GAME. The core gameplay, story, mechanics, scenes, and overall experience are already developed. At this stage, I do not want new chapters, unnecessary mechanics, experimental features, or major changes to the game's design.

We are entering the final polishing and release-preparation phase.

Your responsibility is to carefully complete every task below, verify that everything works, document the entire project, and push the final changes to Git.

**Take your time, inspect the existing project, understand how everything is connected, and implement these changes properly. Do not rush, take shortcuts, or break existing functionality.**

---

# 1. IMPORTANT RULES BEFORE STARTING

Before modifying anything, inspect the current project and understand its existing architecture.

You have permission to inspect, modify, test, and improve the project as necessary to complete this task.

However, follow these rules:

1. **Preserve all existing gameplay.** Everything that currently works must continue to work after your changes.
2. Do not redesign existing scenes or unnecessarily alter their appearance.
3. Do not remove existing mechanics, scripts, animations, objects, dialogue, or story sequences.
4. Do not introduce duplicate systems when an existing system can be extended.
5. Reuse the existing settings architecture, input system, UI conventions, scene-management systems, and player controllers wherever possible.
6. Avoid unnecessary dependencies and external packages.
7. Do not replace working systems with simplified alternatives.
8. Do not modify the grocery store scene or its cashier transformation sequence. That area has already been completed and should remain untouched unless an existing dependency must be adjusted for compatibility. Any unavoidable change must be minimal.
9. Do not commit broken, unfinished, or untested changes.
10. If you encounter an existing bug that directly prevents one of the requested features from working, investigate and fix it carefully without expanding the scope.

**Most importantly: inspect before changing.** Understand the relationship between the existing `City` scene, `CarDriving` scene, their controllers, cameras, lighting, transitions, and related scripts.

Do not assume that similarly named objects or scripts perform the same functions.

---

# 2. CARDRIVING SCENE — NAVIGATION ARROWS AND DESTINATION VISIBILITY

The first major task is improving navigation in the `CarDriving` scene.

The atmosphere is intentionally extremely dark. I actually like the darkness, and I do NOT want the environment brightened just to make navigation easier.

However, the darkness makes it extremely difficult to identify where the player is supposed to go.

I want a directional guidance system.

## 2.1. Directional arrows

When the player enters the `CarDriving` scene, the game should display directional arrows that guide the player toward the relevant destination or objective.

First, investigate the existing story progression and driving sequence to identify exactly which object, location, or destination the player is supposed to reach.

Use the existing objective or destination system if one already exists. Do not arbitrarily select a different target.

The navigation system should:

- Clearly indicate the direction of the intended destination.
- Use visible arrows that point toward the destination.
- Update their orientation as the player's vehicle moves and changes direction.
- Continue pointing toward the correct destination regardless of the vehicle's position or rotation.
- Remain properly synchronized with the player's camera and vehicle.
- Avoid pointing in the wrong direction when the vehicle turns.
- Avoid becoming detached from the player's perspective.
- Work reliably throughout the driving sequence.
- Disappear or deactivate when navigation is no longer relevant, such as when the destination is reached or the driving sequence ends.

The arrows should be visually noticeable without destroying the horror atmosphere.

**The intended experience:** The player should remain immersed in the dark environment while still having a clear indication of where to drive.

Do not make the navigation system look like a generic racing-game interface. It should fit the existing visual identity of PARALYSIS.

## 2.2. Destination glow

The destination itself should also become easier to identify.

Implement a glow or emissive visual effect on the relevant destination object.

Requirements:

- The destination should be distinguishable from its surroundings.
- The glow must remain visible in the extremely dark environment.
- It should be noticeable from an appropriate distance.
- It must not illuminate the entire environment unnecessarily.
- It must not turn the destination into an overwhelmingly bright object.
- The effect should remain visually consistent with the game's atmosphere.
- The effect must work with the existing lighting, materials, and rendering configuration.

If necessary, use an emissive material, a controlled light source, or another suitable rendering technique.

Do not simply increase the brightness of every object in the scene.

The goal is to make the destination identifiable while preserving the darkness.

## 2.3. Technical implementation

Make the navigation system reliable rather than implementing arrows that only work under one specific camera angle.

Check for:

- Correct target references.
- Correct world-space and local-space orientation.
- Vehicle rotation.
- Camera rotation.
- Target visibility.
- Distance calculations.
- Scene transitions.
- Missing references or destroyed objects.
- Performance overhead.

Avoid unnecessary update calculations where possible.

If the target is not directly visible, the directional guidance should still work.

If the target cannot be found, fail gracefully rather than generating console errors or breaking the driving sequence.

---

# 3. CITY AND CARDRIVING SCENE — COMPLETE ENVIRONMENT SYNCHRONIZATION

THIS IS EXTREMELY IMPORTANT.

The `City` and `CarDriving` scenes must be synchronized.

I want the driving environment to match the actual city environment as closely as possible.

**Treat the existing `City` scene as the source of truth for the shared city environment.**

Inspect both scenes carefully and identify all the relevant differences.

Then synchronize the environments.

## 3.1. What needs to be synchronized

I want the shared city environment from `City` properly reproduced or integrated into `CarDriving`.

This includes, where applicable:

- Buildings.
- Roads.
- Sidewalks.
- Terrain.
- Environmental objects.
- Street objects.
- Decorations.
- Background geometry.
- Environmental structures.
- Shared props.
- Materials and textures.
- Skybox and atmospheric elements.
- Lighting and environmental settings.
- Other visual elements that establish the city's appearance.

Do not leave important environmental objects behind.

The objective is to make the city in `CarDriving` look like the same physical location as the city in `City`, rather than two separate interpretations of the same environment.

## 3.2. Preserve the driving functionality

There is one very important distinction.

I want the **environment** fully synchronized, not the driving system replaced.

The `CarDriving` scene already contains its own working vehicle-related functionality.

Preserve:

- The existing vehicle controller.
- Vehicle movement.
- Steering.
- Acceleration and braking.
- Vehicle physics.
- The driving camera.
- Camera behavior.
- Vehicle-related scripts.
- Existing driving interactions.
- Scene transitions.
- Existing references and dependencies.

Do not accidentally replace the driving controller with the on-foot controller from `City`.

Do not introduce duplicate players, cameras, audio listeners, event systems, or other objects that conflict with existing functionality.

The final result should combine the city environment from `City` with the working driving functionality of `CarDriving`.

## 3.3. Scene synchronization quality control

After synchronization, inspect the result carefully.

Check:

- Object positions and rotations.
- Scale consistency.
- Terrain alignment.
- Road connectivity.
- Missing materials.
- Missing textures.
- Duplicate geometry.
- Unexpected lighting differences.
- Clipping.
- Floating objects.
- Camera clipping.
- Physics collisions.
- Vehicle spawn position.
- Vehicle movement through the environment.
- Any broken references created by copying objects.

Make sure the vehicle can actually drive through the resulting environment.

If the city uses static objects, colliders, or other environment-specific components, preserve or recreate the necessary components.

Do not copy objects blindly if doing so creates duplicate systems or invalid references.

The result should look coherent and intentional.

## 3.4. Maintainability

Where possible, keep the environment synchronization manageable.

Avoid creating an unnecessarily complicated hierarchy.

Use clear object names and preserve the project's existing naming conventions.

Do not create a second collection of unnecessary scripts just to reproduce existing functionality.

---

# 4. SETTINGS MENU — VERTICAL SYNC (VSYNC)

Next, improve the graphics settings.

I want the player to be able to enable or disable VSync directly from the existing Settings menu.

Do not create a separate settings screen.

Integrate everything into the current settings interface.

## 4.1. VSync functionality

Add a VSync option with at least two states:

- Enabled.
- Disabled.

When the player changes this setting, it should actually change the game's rendering behavior.

Use Unity's appropriate VSync configuration.

Make sure the option is not merely a visual toggle that does nothing.

## 4.2. FPS compatibility

The project already has an FPS configuration, including its normal performance target and higher frame-rate cap.

Preserve the existing FPS configuration.

VSync and the FPS limiter must behave sensibly together.

Pay particular attention to Unity's interaction between VSync and `Application.targetFrameRate`.

Do not accidentally disable the existing FPS cap or introduce contradictory settings.

Test both configurations:

- VSync disabled.
- VSync enabled.

Make sure the displayed setting correctly represents the actual configuration.

---

# 5. SETTINGS MENU — RESOLUTION OPTIONS, UP TO 4K

Add a resolution selection option to the existing Settings menu.

The player should be able to change the game's display resolution without restarting the entire game.

## 5.1. Resolution selection

Implement a proper resolution dropdown or another suitable selection interface.

The available resolutions should be obtained from the supported display resolutions rather than blindly displaying resolutions that the current display cannot use.

The maximum resolution offered must be **4K UHD (3840 × 2160)**.

Include sensible lower resolutions where supported by the display.

Examples of possible options include:

- 1280 × 720.
- 1600 × 900.
- 1920 × 1080.
- 2560 × 1440.
- 3840 × 2160.

These are examples, not a requirement to display unsupported modes.

Do not offer resolutions above the 4K limit.

## 5.2. Applying resolutions

When the player selects a new resolution, apply it correctly.

Ensure:

- The game window resizes appropriately.
- The UI scales correctly.
- The game's aspect ratio is preserved.
- No UI elements become inaccessible.
- No important interface elements are pushed off-screen.
- The selected resolution is accurately represented in the settings menu.

Preserve any existing fullscreen or windowed display behavior.

If the game already has a fullscreen option, make sure the resolution selection works with it.

## 5.3. Confirmation and recovery

Avoid trapping the player in an unusable resolution.

Ideally, after applying a resolution change, display a confirmation prompt.

The player should be able to confirm the new resolution or revert to the previous one if the display becomes unusable.

Use a reasonable timeout for automatic reversion if the player does not confirm.

Make sure this process handles scene changes and settings-menu closure safely.

## 5.4. Persistence

Save the selected resolution using the existing settings-persistence system.

After restarting the game, the selected resolution should remain active, provided it is supported by the current display.

If a saved resolution is no longer supported, fall back to a safe supported resolution.

---

# 6. MOTION BLUR

Implement a proper motion blur effect.

I want motion blur to contribute to the game's visual quality, particularly during movement and driving.

However, it must not make the game unpleasant to play or obscure important visual information.

## 6.1. Implementation

Inspect the current rendering pipeline before implementing anything.

Determine which motion blur solution is compatible with the project's actual rendering configuration.

Do not install an unnecessary rendering package or change the entire rendering pipeline just to add motion blur.

Use the most appropriate solution for the existing project.

The effect should work correctly with:

- The normal player camera.
- The driving camera.
- Camera movement.
- Player movement.
- Vehicle movement.

Pay particular attention to the driving sequence.

The motion blur should feel natural and should not create visual artifacts.

## 6.2. Settings integration

Add a Motion Blur option to the existing Settings menu.

At minimum, provide:

- Enabled.
- Disabled.

Persist the player's selection using the existing settings system.

If the project already supports graphical quality settings, integrate the option consistently with them.

When disabled, motion blur should genuinely be disabled, not merely reduced.

When enabled, the effect should function properly.

Do not add excessive motion blur that makes navigation, driving, or gameplay difficult.

The horror atmosphere should remain the priority.

---

# 7. SETTINGS MENU — GENERAL QUALITY CONTROL

After implementing the graphics settings, inspect the entire Settings menu.

The new options should look like they belong in the existing interface.

Requirements:

- Maintain the current visual style.
- Preserve existing settings.
- Ensure all options are readable.
- Avoid overlapping elements.
- Ensure navigation works with the existing input system.
- Ensure settings actually apply when changed.
- Ensure saved settings load correctly.
- Ensure settings remain consistent after changing scenes.
- Ensure the menu behaves correctly after closing and reopening it.

Do not break mouse sensitivity, audio settings, gameplay settings, or any other existing options.

The existing settings-persistence architecture should remain intact.

Test the settings from the main menu and, where appropriate, while inside gameplay.

---

# 8. COMPLETE README — DOCUMENT THE ENTIRE GAME AND PROJECT

Now comes a VERY important part.

I want a **fully descriptive README.md** in the root directory of the project.

This is not supposed to be a two-paragraph introduction.

I want someone who has never opened this project to understand what PARALYSIS is, how the game works, how to play it, and what the major components of the project do.

The README should be professionally structured, readable, detailed, and accurate.

**Inspect the actual project before writing it.** Read the relevant scenes, scripts, input configurations, settings, and project files.

Do not invent features or controls.

If a system is implemented, document it. If something is not implemented, do not pretend that it is.

## 8.1. Game introduction

Start with a proper introduction to PARALYSIS.

Include:

- Game title.
- Genre.
- Short description.
- Game premise.
- Overall atmosphere.
- Main gameplay concept.
- The general experience the player should expect.

Explain that PARALYSIS is a horror game centered around a night-time atmosphere, unsettling paranormal experiences, and sleep-paralysis-inspired horror.

Write the introduction professionally without spoiling major story events unnecessarily.

## 8.2. Story and gameplay overview

Describe the overall gameplay progression.

Explain the major stages of the game in their actual implemented order.

Include the relevant gameplay sequences, such as:

- The apartment.
- Grocery preparation and the trip to the store.
- The grocery-store gameplay sequence.
- The cashier interaction.
- Returning from the store.
- The car-related sequence.
- The journey through the city.
- Returning home.
- The paranormal events.
- The sleep-related horror sequences.
- Any other implemented story progression.

Only describe sequences that are actually present in the final project.

Explain the purpose of the major objectives and how the player progresses.

Avoid unnecessarily revealing important scares or endings without warning.

## 8.3. HOW TO PLAY — COMPLETE PLAYER GUIDE

This section must be especially detailed.

Explain how to play the game from beginning to end.

Include every relevant control and interaction.

Inspect the actual input configuration and scripts to identify the correct keys and actions.

Document:

**Player movement**
- Forward movement.
- Backward movement.
- Left and right movement.
- Sprinting, if implemented.
- Jumping, if implemented.
- Crouching, if implemented.
- Camera movement and mouse look.

**Interactions**
- The interaction key.
- How interactable objects work.
- How object descriptions are displayed.
- How camera zoom works during interactions.
- How the player exits an interaction.
- Whether interactions can produce sounds or dialogue.

**Dialogue**
- How dialogue appears.
- How the player advances dialogue.
- Any other relevant dialogue functionality.

**Driving**
- How to enter and operate the vehicle.
- Acceleration.
- Braking.
- Steering.
- Any other available vehicle controls.
- How the driving objective is communicated.
- How the player reaches the intended destination.

**Objectives**
- How objectives are displayed.
- How objectives progress.
- How the player knows what to do next.

**Menus**
- Main menu.
- New Game.
- Continue.
- Chapter selection, if available.
- Pause menu.
- Settings.
- How to resume gameplay.
- How to exit the game.

**Graphics and settings**
- Mouse sensitivity.
- VSync.
- Resolution.
- Motion blur.
- FPS settings.
- Other available settings.

Do not use guessed controls.

If the actual implementation differs from these examples, document what the game actually supports.

The purpose of this section is to make sure a completely new player can understand and operate the game without needing to ask the developer.

## 8.4. Game mechanics and systems

Document the major mechanics in detail.

Explain what each system does and how it contributes to gameplay.

Include all relevant implemented systems, such as:

- First-person player controller.
- Camera controls.
- Interaction system.
- Dialogue system.
- Objective system.
- Lighting system.
- Scene transitions.
- Teleportation systems.
- Vehicle controller.
- Driving system.
- Navigation and destination indicators.
- Store gameplay systems.
- Cashier interaction and transformation sequence.
- Horror events.
- Sound effects.
- Music.
- Settings persistence.
- Save and continue functionality.
- Pause system.
- Any other significant gameplay systems.

For each system, explain:

1. What it does.
2. How it works from the player's perspective.
3. Which important features it provides.
4. How it interacts with other systems, where relevant.

Do not just list system names. Actually explain their functionality.

## 8.5. Scene documentation

Document the important Unity scenes.

For each relevant scene, explain:

- Its purpose.
- What happens inside it.
- Which major systems it uses.
- How the player enters it.
- How the player leaves it.
- Any important dependencies.

Include the implemented scenes such as `Home`, `City`, `CarDriving`, the store scene, menu scenes, or other actual project scenes.

Inspect the actual scene files and do not invent scene names.

Explain the relationship between the on-foot city experience and the driving experience.

## 8.6. Script and project architecture

This section should help another developer understand the codebase.

Inspect the project's actual scripts and organize the important ones into logical categories.

For significant scripts, document:

- Script filename.
- Its responsibility.
- Its major functionality.
- Important dependencies.
- Which scene or system uses it.

Cover the major areas of the project, including:

- Player movement.
- Camera systems.
- Interactions.
- Dialogue.
- Objectives.
- Scene management.
- Teleportation.
- Vehicle movement.
- Driving.
- Store systems.
- Lighting.
- Settings.
- Saving and persistence.
- UI.
- Audio.
- Other important systems.

Also explain the overall project organization and important asset directories where useful.

Do not generate a meaningless list of every Unity asset or every imported library file.

Focus on the actual systems that make the game function.

The README should give developers a useful understanding of how the project is organized.

## 8.7. Settings documentation

Explain every available settings option.

For each option, describe:

- What it controls.
- What happens when it is enabled or changed.
- Whether its value is saved.
- Any relevant limitations.

Make sure the newly added VSync, resolution, and motion blur options are documented accurately.

## 8.8. Installation and project setup

Document how someone can open the project and run it.

Inspect the project configuration and determine the correct Unity version and any required dependencies.

Include:

- Required software.
- Unity version.
- How to open the project.
- How to load the correct scene or launch the game.
- How to run it in the Unity Editor.
- Any required setup steps.
- Build instructions, where applicable.

Do not invent Unity versions or dependencies.

## 8.9. Troubleshooting

Include a troubleshooting section covering common issues.

Examples:

- Missing or unsupported display resolutions.
- Settings not appearing to apply.
- Camera or mouse input problems.
- Missing scene references.
- Missing assets or packages.
- Performance issues.
- Other likely problems identified from the actual project.

Only include useful and relevant information.

## 8.10. Credits and AI disclosure

Include a small credits section at the very end.

Keep the AI disclosure brief and honest.

The final note should communicate:

**"I used AI just to code."**

Do not exaggerate the role of AI or imply that it independently created the entire game.

Keep this acknowledgment short. The README should primarily document the game and project.

## 8.11. README quality requirements

The final README must:

- Be written in proper Markdown.
- Use clear headings and subheadings.
- Include a table of contents if appropriate.
- Be professionally formatted.
- Be descriptive without becoming unnecessarily repetitive.
- Use accurate controls and technical details.
- Avoid unsupported claims.
- Be easy for both players and developers to understand.

I want this to be a genuinely useful document, not something generated just to fill a file.

---

# 9. FINAL TESTING AND VERIFICATION

After completing all implementations and documentation, perform a proper final verification.

Do not assume that something works simply because the code compiles.

Test the actual functionality.

## 9.1. Scene verification

Verify:

- `City` still works correctly.
- `CarDriving` still works correctly.
- The synchronized city environment is present in `CarDriving`.
- The driving controller remains functional.
- The vehicle can move through the environment.
- The vehicle camera behaves correctly.
- Scene transitions still work.
- No duplicate cameras or players cause issues.
- No important scene references are missing.

## 9.2. Navigation verification

Verify:

- Navigation arrows appear in the correct scene.
- Arrows point toward the correct destination.
- Arrows update when the vehicle changes direction.
- The destination has the intended glow.
- The effect remains visible in the dark.
- The horror atmosphere is preserved.
- Navigation ends when it should.
- There are no missing-reference errors.

## 9.3. Graphics settings verification

Test:

- VSync can be enabled.
- VSync can be disabled.
- Resolution can be changed.
- 4K is the maximum selectable resolution.
- Unsupported resolutions are handled properly.
- Resolution confirmation and reversion work.
- Motion blur can be enabled.
- Motion blur can be disabled.
- Settings persist after restarting.
- Existing settings still work.
- UI elements remain correctly positioned at different resolutions.

## 9.4. Regression testing

Check that existing gameplay has not been damaged.

In particular, preserve:

- Main menu functionality.
- New Game.
- Continue.
- Chapter selection.
- Pause menu.
- Settings menu.
- Dialogue.
- Interactions.
- Objectives.
- Lighting.
- Teleportation.
- Store gameplay.
- Cashier sequence.
- Horror events.
- Audio.
- Existing story progression.

Do not modify existing gameplay simply to make the new features easier to implement.

## 9.5. Console and project health

Inspect the Unity Console and relevant project logs.

Resolve errors introduced by your changes.

Pay attention to:

- Null references.
- Missing components.
- Missing scripts.
- Invalid serialized references.
- Duplicate systems.
- Rendering errors.
- Scene-loading problems.
- Incorrect settings.
- Broken prefabs.
- Compilation errors.

Do not hide errors by disabling warnings or removing useful logging.

Distinguish between pre-existing issues and new issues caused by your changes.

The goal is a stable final project.

---

# 10. FINAL GIT COMMIT AND PUSH

THIS IS THE LAST GIT PUSH FOR THE PROJECT.

After everything is implemented, tested, and documented, prepare and push the final changes to the existing Git remote.

**Do this LAST.**

Do not push halfway through development.

## 10.1. Before committing

Inspect:

- Current Git branch.
- Current repository status.
- Existing remote configuration.
- Uncommitted changes.
- Changes introduced during this task.

Be careful not to accidentally overwrite unrelated existing work.

Do not discard user changes.

Do not reset the repository.

Do not rewrite Git history.

Do not force-push.

## 10.2. Commit the final changes

Stage the appropriate project changes, including:

- Navigation arrow system.
- Destination glow.
- City and CarDriving synchronization.
- Graphics settings.
- VSync functionality.
- Resolution selection.
- Motion blur.
- README documentation.
- Necessary configuration changes.

Follow the project's existing Git conventions where possible.

Create a meaningful final commit message.

For example:

`Final game polish, graphics settings, scene synchronization, and documentation`

Use a different message if it better describes the actual changes.

## 10.3. Push to the existing remote

Push the final commit to the correct existing remote and branch.

Verify that the push succeeded.

If the remote rejects the push or there is a conflict, investigate safely.

Do not force-push to bypass the problem.

If pushing is impossible because of authentication, network, or remote configuration problems, explain exactly what happened. Do not claim that the changes were pushed when they were not.

## 10.4. Final Git verification

After pushing, verify:

- The intended commit exists.
- The intended branch contains the commit.
- The push completed successfully.
- The working tree is clean, apart from any explicitly documented generated or intentionally untracked files.

Report the final commit hash.

---

# 11. FINAL REPORT

When everything is finished, provide a final report.

I want a concise but informative summary containing:

**1. Navigation system**
- What was implemented.
- How the arrows work.
- How the destination glow works.

**2. Scene synchronization**
- What was copied or synchronized from `City`.
- How the driving functionality was preserved.
- Any relevant technical decisions.

**3. Graphics settings**
- VSync.
- Resolution options and 4K limit.
- Motion blur.
- Settings persistence.

**4. README**
- Confirm that `README.md` was created or updated.
- Summarize the sections documented.

**5. Testing**
- What was actually tested.
- Whether the project compiled.
- Whether the important gameplay systems were verified.
- Any remaining issues or limitations.

**6. Git**
- Commit message.
- Commit hash.
- Branch.
- Whether the push succeeded.

Be completely honest about testing. Do not claim to have tested something that you could not actually run or verify.

If anything remains unfinished, state it clearly.

---

# 12. FINAL MESSAGE

And finally...

We have worked on this game for a long time. There have been bugs, broken scenes, complicated scripts, endless debugging, and plenty of unexpected problems.

But we made it.

The goal of this final task is to leave PARALYSIS in a clean, stable, documented, and properly synchronized state.

Do not turn this into another major development cycle.

Do not add unnecessary features.

Do not redesign the game.

Finish the requested tasks, verify everything, write the README, and push the final Git commit.

**After this, the game is officially finished.**

Be proud of the work, because we made an entire game.

Thank you for your work, comrade. 🫡

**NOW LET'S SHIP PARALYSIS.**