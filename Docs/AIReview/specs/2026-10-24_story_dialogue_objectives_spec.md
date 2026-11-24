# Spec written by the developer (Serali / ciklole1)

Sent to the AI agent on 2026-10-24 21:41 UTC, 46545 characters, in the chat session.
Reproduced here verbatim from the session history.
---

Now THIS..... IS MEGA BIG # PARALYSIS — DETAILED STORY, DIALOGUE, OBJECTIVE, AND INTERACTION SPECIFICATION

## IMPORTANT: READ BEFORE MODIFYING THE PROJECT

You are working on an existing Unity horror game project called **Paralysis**.

This is an evolving project. The developer expects to change dialogue, scenes, UI, objectives, interactions, timings, sounds, and story events frequently.

Your implementation must therefore prioritize:
- Inspector configurability.
- Reusable systems.
- Clear separation of responsibilities.
- Compatibility with existing scripts.
- Easy future modifications.
- Reliable event sequencing.
- No unnecessary rewrites.

Do not treat the current specification as a permanent, hardcoded story. Treat it as the initial configuration of a reusable system.

### Existing scripts identified by the developer

The project already contains the following scripts, or versions of them:

- `UIHelper.cs`
- `DialogueSystem.cs`
- `DialogueTrigger.cs`
- `InteractableObject.cs`
- `ObjectiveManager.cs`
- `QuestLogUI.cs`
- `ShoppingCart.cs`
- `GroceryItem.cs`
- `LightsObjective.cs`
- `ObjectiveTrigger.cs`

These names come from the developer's current project overview. Inspect the actual files before making changes.

The project also has existing apartment lighting, scene transitions, teleportation, and other gameplay components.

**Do not create replacement systems before checking whether the existing implementations can be extended.**

Before modifying any files:
1. Inspect the relevant scripts.
2. Identify current references and dependencies.
3. Identify which systems survive scene changes.
4. Check for existing input handling and the Unity Input System configuration.
5. Check how the lighting system exposes the state of each light.
6. Check how the player, camera, and scene transitions currently work.
7. Check whether the UI helper already creates a Canvas and EventSystem.
8. Make a list of the minimal changes required.

Do not invent method names or assume APIs exist without checking the code.

---

# PART 1 — THE GAME'S OPENING STORY

## 1.1 Story premise

The main character wants to buy groceries.

The opening begins in the character's apartment at night. The character needs to turn off all the lights, leave the apartment, and get to the car.

The car interaction starts the trip to the grocery store.

At the grocery store, the player receives a shopping list and must find the required items. The player can open the list with M and collect grocery items by pressing F near them.

When the player reaches the cashier, the conversation starts normally. Gradually, the cashier begins behaving strangely.

After the cashier's final dialogue line, the cashier undergoes a brief, disturbing transformation.

The successful shopping path should feel like an ordinary errand that slowly becomes uncomfortable.

Do not introduce unrelated monsters, supernatural explanations, additional characters, or major plot revelations before the cashier transformation unless the developer explicitly adds them later.

## 1.2 Scope of this implementation

Implement and connect the story from the start of the apartment scene through the cashier transformation.

The dialogue content, shopping list, scene configuration, cashier assets, and event timings must be editable.

The implementation should provide a clean extension point for later events, including leaving the store, the car incident, and the return-home sequence. Do not invent or implement additional story outcomes beyond the requested scope.

---

# PART 2 — GENERAL DIALOGUE SYSTEM

## 2.1 Dialogue UI layout

Use the existing `UIHelper.cs` and `DialogueSystem.cs` if their implementations support the requirements. Extend them where necessary.

The dialogue UI must be created automatically at runtime. The developer should not have to manually create a Canvas, panel, or text object for every conversation.

Use one reusable dialogue interface.

Default appearance:

- Canvas: screen-space overlay, unless the existing project requires a different setup.
- Dialogue panel: horizontally centered.
- Panel anchor: centered horizontally and near the bottom of the screen.
- Approximate horizontal width: 80% of the screen.
- Maximum width: approximately 1,200 reference pixels.
- Approximate panel height: 160–210 reference pixels.
- Bottom margin: approximately 45 reference pixels.
- Speaker name: near the upper-left corner of the panel.
- Dialogue text: beneath the speaker name, aligned left.
- Text wrapping: enabled.
- Overflow: do not allow text to escape the panel.
- Background: dark, slightly transparent.
- Text: light and easy to read.
- Default font size: approximately 28 reference pixels for dialogue and 24 for the speaker name.

These are default values, not fixed requirements. Put the relevant visual settings in a configuration object or appropriate Inspector fields.

The layout must scale across different screen resolutions and aspect ratios.

Do not create multiple dialogue panels because a new conversation starts. Reuse the existing UI.

## 2.2 Dialogue line data

Each dialogue line must be editable without rewriting the dialogue system.

At minimum, support these fields:

- Speaker name.
- Dialogue text.
- Optional voice audio clip.
- Optional separate sound effect.
- Delay before the line becomes available.
- Whether the typewriter effect is enabled for this line.
- Optional event to invoke after the line finishes.
- Optional pause after the line.
- Optional setting to control player movement during the line, if supported by the existing architecture.

Use serializable data that can be edited in the Inspector.

Prefer a reusable dialogue entry or dialogue sequence asset if appropriate. Do not create a different custom MonoBehaviour for every individual sentence.

## 2.3 Typewriter effect

Use these default settings:

- Initial typewriter speed: 35 characters per second.
- Initial dialogue delay: 0 seconds, unless the specific story beat below says otherwise.
- Text must wrap correctly.
- Punctuation should remain visible.
- A long sentence must not exceed the panel boundaries.
- Do not automatically hide a dialogue line merely because a timer expires.

When the player left-clicks:

1. If the current line is still typing, finish displaying that line immediately.
2. Do not advance to the next line on that same click.
3. On a subsequent left-click, advance to the next line.
4. If the current line has finished typing, advance normally.
5. If the final line has finished typing, the next click ends the conversation and allows the configured completion event to occur.

This behavior must be consistent across conversations.

A mouse click used to advance dialogue must not also activate a grocery item, a car, a light switch, a door, or an unrelated interaction.

Do not treat a click held down as repeated clicks.

Provide a configurable way to disable the typewriter effect globally or for individual lines.

## 2.4 Input ownership

The game contains multiple actions:

- Left-click: advance dialogue.
- E: interact with the car or a configured environmental object.
- F: inspect or collect an object.
- M: toggle the mission log.
- Esc: exit inspection or perform another explicitly configured action.

These actions must not accidentally activate multiple systems at once.

When a conversation is active:
- Dialogue advancement owns left-click input.
- Other interaction systems must not respond to the same input event.
- Do not let the player collect a grocery item halfway through a dialogue unless the developer deliberately permits that behavior.
- Do not allow a second conversation to start in the background.
- Decide consistently whether gameplay movement is locked for a conversation.
- Default to locking player movement during the initial opening dialogue and the cashier conversation.
- For optional short comments elsewhere, make the movement-lock setting configurable.

When inspection is active, the inspection system should own the relevant interaction key until inspection ends.

When the mission log is open, M should close it.

Do not automatically close another UI merely because a different interaction was attempted unless that behavior is explicitly configured.

If the project already uses Unity's Input System, integrate with its existing input actions rather than creating an incompatible parallel input system.

## 2.5 Optional audio

A dialogue line may have an optional voice clip.

If assigned, play the clip at the correct point in the dialogue.

If no voice clip is assigned, the dialogue must still work normally.

Provide an optional sound-effect field for scripted moments.

Do not play the same voice clip repeatedly because the typewriter effect updates the text every frame.

Support separate audio sources or another appropriate setup for dialogue, voice, and environmental effects. Inspect the existing audio architecture first.

A missing optional audio clip must never cause a null-reference exception.

---

# PART 3 — STORY EVENT A: BEGINNING IN THE APARTMENT

## A.1 Trigger

Trigger the opening conversation once the opening scene has loaded, the player reference has been resolved, and the player is ready to play.

Do not rely on an arbitrary `Start()` call if the player or camera is initialized asynchronously.

Default initial delay after the player is ready: 1.0 second.

This delay is configurable.

Do not replay the opening conversation after the player interacts with an object or returns to the scene unless the scene has deliberately been reset.

## A.2 Initial atmosphere

During the initial delay:

- Show the apartment normally.
- Allow the existing ambient audio to play.
- Do not trigger the cashier sequence or any other grocery-store event.
- Do not add an unrequested jumpscare.
- Do not display an unnecessary tutorial overlay.

After the initial delay, display the first dialogue line.

## A.3 Exact dialogue

Conversation ID: `APT_Opening_01`

Line 1:

Speaker: `You`

Text:

"Alright. Let's get this over with."

Line 2:

Speaker: `You`

Text:

"I should probably get some groceries before it gets too late."

Line 3:

Speaker: `You`

Text:

"Where did I put that shopping list?"

Line 4:

Speaker: `You`

Text:

"Ah, there it is. Bread, milk, eggs... I think that's everything."

These lines should appear in that order.

The player advances each line with a left-click. There is no automatic line advancement.

Default optional pause after each line: 0.25 seconds, where the existing dialogue implementation can support it without interfering with the required click behavior.

## A.4 Conversation completion

When the player finishes the final line and ends the conversation:

1. Hide the dialogue panel.
2. Release the dialogue system's input lock.
3. Wait 0.5 seconds.
4. Activate objective 1.
5. Show the objective notification.

Do not activate the objective before the conversation is finished.

If dialogue is disabled, skipped, or intentionally omitted, objective activation must still occur.

## A.5 Objective 1

Exact objective title:

`Turn off all the lights in the house`

Optional notification text:

`OBJECTIVE UPDATED`

Show the active objective in the existing top-left objective HUD.

The mission log should also record it.

Do not immediately complete this objective because the opening scene loads or because the player touches a light switch.

The completion condition is defined in Part 4.

---

# PART 4 — OBJECTIVE 1: TURN OFF ALL LIGHTS

## 4.1 Light tracking

Reuse `LightsObjective.cs` and the existing light system.

Do not implement an unrelated light-switching system just for this objective.

Determine how the existing light switches expose their state and how lights are registered.

The objective must work with the actual project lighting setup.

Provide configurable references to the light switches that count toward this objective.

If the project already has a reliable registration system, use that system.

Do not assume every Light component in the scene is one of the required lights. Decorative lights, headlights, streetlights, and other unrelated lights should not accidentally block objective completion.

## 4.2 Required completion logic

The objective completes only when all configured required light switches are off.

For the initial objective:

`allRequiredLightsOff == true`

The objective must not complete if:
- One or more required lights are still on.
- A required reference is missing and the system cannot determine completion reliably.
- The player has turned off only one light while others remain on.

When the final required light is turned off:

1. Verify the entire set of required lights.
2. Mark objective 1 complete exactly once.
3. Display a completion notification for approximately 2 seconds.
4. Wait approximately 0.5 seconds after the notification begins.
5. Start the optional dialogue line below.
6. After the dialogue ends, activate objective 2.

Optional dialogue:

Speaker: `You`

Text:

"Finally. Let's go."

Make this optional through a configuration setting.

If optional dialogue is disabled, objective 2 should activate automatically after the short configured transition delay.

## 4.3 Objective 2

Exact title:

`Go outside to the car`

Use the existing apartment exit and scene-transition system.

The objective should not complete just because the player walks near a door.

It should complete when the appropriate exit interaction or successful scene transition occurs.

Use the existing apartment door logic if it already handles this.

Do not create a second scene-transition mechanism that conflicts with the current one.

If a confirmation or loading period is required, preserve the current project conventions.

---

# PART 5 — STORY EVENT B: THE CAR

## 5.1 Scene configuration

The developer should be able to choose the relevant scene names in the Inspector.

Create configurable references or settings for the equivalent of:

- Apartment scene.
- Exterior/city scene.
- Grocery-store scene.

The names are examples of configuration roles, not guaranteed actual scene names.

Inspect the project's actual scenes and Build Settings before assuming any scene names.

Do not hardcode a particular scene name inside multiple scripts.

Use one authoritative configuration source for scene identifiers wherever practical.

Validate scene names or references and provide useful error messages if a required scene is missing.

## 5.2 Car interaction trigger

In the configured exterior/city scene:

- The player approaches the car.
- An interaction prompt appears when the player is within the configured interaction range.
- Default key: E.
- Default prompt: `E — Interact`.

The prompt should disappear when the player leaves the interaction range.

Use the project's existing interaction and player-reference systems if possible.

Do not allow the car to be activated repeatedly while the transition is already in progress.

## 5.3 Car dialogue

When the player presses E while the car is a valid interaction target, start conversation ID `CAR_Interaction_01`.

Line 1:

Speaker: `You`

Text:

"Now, where's that list? I need to make sure I get everything."

Use the normal dialogue UI and click behavior.

After the conversation finishes:

1. Close the dialogue panel.
2. Release the input lock.
3. Activate the next objective.
4. Allow the configured car interaction to proceed using the existing project setup.

## 5.4 Objective 3

Exact title:

`Drive to the grocery store`

This objective must not activate merely because the exterior scene loaded.

It activates when the player interacts with the car and finishes the associated dialogue.

If actual driving mechanics already exist, integrate with them.

If actual driving is not implemented, do not fabricate an elaborate vehicle-controller system as part of the dialogue task. Provide a configurable action or transition hook so the developer can connect the car interaction to the intended gameplay.

The objective completes only when the project registers arrival at the configured grocery-store scene or another explicitly configured arrival event.

A scene transition should not accidentally complete the objective if the player enters the grocery store through an unrelated debug shortcut, unless that is deliberately supported.

The normal gameplay path should complete it.

---

# PART 6 — STORY EVENT C: ARRIVING AT THE GROCERY STORE

## 6.1 Scene-entry detection

When the player enters the configured grocery-store scene through the intended trip:

1. Resolve the player and required managers.
2. Confirm that the trip objective is active or appropriately completed.
3. Complete objective 3 exactly once.
4. Prepare the shopping list.
5. Start the grocery-store opening dialogue when the scene and player are ready.

Do not start the dialogue before the UI and player references are available.

Default delay after scene readiness: 0.75 seconds.

Make this configurable.

If the player enters the scene through an editor test or debug shortcut, the system should still be possible to test without requiring the entire apartment sequence. Provide a deliberate testing or initialization method rather than adding random fallback behavior.

## 6.2 Exact arrival dialogue

Conversation ID: `STORE_Opening_01`

Line 1:

Speaker: `You`

Text:

"Alright. Let's see what we need."

Line 2:

Speaker: `You`

Text:

"Bread, milk, eggs... should be easy enough."

Use the normal player-click dialogue system.

After the final line ends, show the instruction:

`Press M to view your shopping list. Find each item and press F to collect it.`

Show the instruction for approximately 5 seconds by default, then hide it automatically.

Do not require the player to click the instruction to dismiss it.

The instruction duration should be configurable.

## 6.3 Automatic shopping-list opening

The developer wants the mission log to open automatically when transitioning into the designated scene, and to be manually accessible with M.

Implement configurable automatic opening.

Preferred behavior:
- Entering the designated grocery-store scene marks the shopping list as pending automatic display.
- If dialogue is active, queue the automatic opening.
- When the opening dialogue finishes, automatically open the mission log.
- The first time it opens, show the grocery list tab or equivalent shopping-list view.
- The player can close it using M.
- The player can later reopen it using M.

The mission log must not spawn a new panel every time it opens.

The mission log should always present the correct current state when reopened.

The automatic-opening behavior must be configurable, and the target scene must be configurable. Do not hardcode a scene name into `QuestLogUI.cs`.

If the existing project intentionally uses a different opening order, adapt the sequence while ensuring the player sees both the intro dialogue and shopping list without conflicting UI.

---

# PART 7 — SHOPPING LIST AND GROCERY SYSTEM

## 7.1 Shopping list contents

Create a configurable shopping-list data structure.

Each required entry must contain at least:

- A stable item identifier.
- Display name.
- Required quantity, default 1.
- Number collected.
- Optional icon.
- Optional short description.
- Optional pickup sound.
- Optional display ordering.

The actual 3D grocery item must reference the corresponding stable item identifier.

Do not identify grocery items solely by their displayed names, because the developer may rename items later.

For example, changing the display name from `Milk` to `Carton of Milk` must not break item matching.

## 7.2 Default test shopping list

Use the following only as initial test data:

1. Bread — quantity 1.
2. Milk — quantity 1.
3. Eggs — quantity 1.

The developer must be able to change these entries in the Inspector.

Do not permanently embed these three items into the source code.

If the project includes more grocery items, preserve them when possible.

If no grocery models exist yet, create configurable item placeholders or document the missing assignments. Do not invent asset paths or reference nonexistent prefabs.

## 7.3 Shopping-list UI

Reuse `QuestLogUI.cs` and `ShoppingCart.cs` wherever appropriate.

When the list is open, show:
- A clear title such as `SHOPPING LIST`.
- Each required item.
- Its collected quantity and required quantity.
- A visual distinction between incomplete and completed entries.
- The current collection progress.
- A clear way to see whether shopping is complete.

Example:

`SHOPPING LIST`

`Bread        0 / 1`

`Milk         0 / 1`

`Eggs         0 / 1`

`PROGRESS: 0 / 3`

When an item is collected, update the UI immediately.

For quantities greater than 1, count the required number of valid items rather than marking the entire entry complete after the first pickup.

Do not allow the same unique grocery object to be collected repeatedly unless duplicate collection is intentionally supported.

The UI should not lose its progress when the player closes it.

## 7.4 Grocery interaction

Reuse `GroceryItem.cs` if possible.

When the player enters the valid interaction radius of a grocery object:
- Show a prompt such as `F — Add to cart`.
- Hide the prompt when the object is no longer a valid interaction target.
- Use the existing player-interaction architecture rather than creating an independent key listener on every item.

Default interaction key: F.

When the player presses F:
1. Check whether dialogue or another modal interaction currently owns input.
2. Check whether this grocery object is in range.
3. Check whether it has already been collected.
4. Resolve its stable item identifier.
5. Check whether that item is in the current shopping requirements.
6. If valid, record the collection once.
7. Update the shopping list.
8. Play any assigned sound.
9. Hide or mark the collected object appropriately.
10. Update the overall progress.

If the item is not required, do not silently count it toward the shopping list.

Default optional response for an unnecessary item:

Speaker: `You`

Text:

"I don't think I need that."

Make this response configurable and prevent it from being spammed by repeated button presses.

If a required item is already collected, pressing F should not add it again.

## 7.5 Collection dialogue

After the first successful required pickup, optionally display:

Speaker: `You`

Text:

"One down."

After all required quantities are collected for the first time, optionally display:

Speaker: `You`

Text:

"That's everything. Time to pay."

Both lines must be triggered once per shopping session.

Do not repeat either line merely because the mission log is reopened.

If the dialogue system is busy, queue the optional line or omit it according to an Inspector setting. Never start two dialogue sequences simultaneously.

## 7.6 Shopping completion state

Define a single authoritative check for shopping completion.

The requirements must all be satisfied:

- Every required item identifier is present in the collection records.
- Every required quantity has been met.
- No pending collection operation is still processing.

Only then should shopping be marked complete.

The cashier interaction is the next intended story event.

Do not complete shopping merely because the player collects any three objects.

The required item identities and quantities matter.

---

# PART 8 — CASHIER INTERACTION

## 8.1 Cashier configuration

Use a configurable cashier interaction component or extend the project's existing interaction system.

The cashier must support:
- A configurable interaction key, default E.
- A configurable interaction radius.
- A configurable prompt.
- References to the relevant dialogue sequences.
- References to the transformation assets.
- References to audio and animation events.
- A checkout-completion event.
- A failure-path event.

The cashier must not start the conversation every frame while the player remains within the interaction radius.

## 8.2 When the player interacts

Default prompt:

`E — Talk to cashier`

If shopping is complete:
- Start the successful checkout conversation.
- Lock movement if configured.
- Prevent grocery pickup and other interactions during the conversation.
- Do not start a second checkout sequence.

If shopping is incomplete:
- Run the incomplete-checkout behavior defined in Part 10.
- Do not mark the checkout as successful.
- Do not silently skip the missing items.

The cashier's appearance and behavior should initially seem ordinary.

Do not play the monster transformation before the ordinary checkout dialogue finishes.

---

# PART 9 — EXACT CASHIER DIALOGUE AND TRANSFORMATION

## 9.1 General presentation

Conversation ID: `CASHIER_Success_01`

The purpose is to make the player uncomfortable gradually.

The cashier should not immediately talk like a monster. The first lines must sound like a normal employee dealing with a customer.

Keep the atmosphere subtle at first.

Use the dialogue system's normal left-click progression. Do not make the cashier's dialogue advance automatically.

The optional delays below control atmospheric pauses and event timing. They do not replace the player's click input.

## 9.2 Opening dialogue

Line 1:

Speaker: `Cashier`

Text:

"Evening. Did you find everything you were looking for?"

Line 2:

Speaker: `You`

Text:

"Yeah. I got everything on the list."

Line 3:

Speaker: `Cashier`

Text:

"Good. Let's get you checked out."

After Line 3 has finished, play a short sequence of grocery-scanner sounds.

Default scanner sequence duration: 1.5 seconds.

The sequence must be configurable, and it must not hold the game indefinitely if an audio clip is missing.

After the scanner sequence, begin the next line.

## 9.3 First uncomfortable exchange

Line 4:

Speaker: `Cashier`

Text:

"You live around here?"

Line 5:

Speaker: `You`

Text:

"Yeah. A few streets away."

Line 6:

Speaker: `Cashier`

Text:

"Must be quiet at night."

Line 7:

Speaker: `You`

Text:

"Usually."

Line 8:

Speaker: `Cashier`

Text:

"Not tonight?"

Line 9:

Speaker: `You`

Text:

"What?"

Line 10:

Speaker: `Cashier`

Text:

"Nothing. Just making conversation."

At the end of Line 10, wait approximately 0.8 seconds before revealing the next line.

Default input behavior remains click-to-advance. The delay happens before the next line becomes available.

During this section:
- The cashier can continue ordinary idle animations.
- The supermarket ambience should remain mostly normal.
- No transformation should occur.
- No loud sting should occur.
- Do not abruptly move the camera unless the existing interaction system is deliberately configured to do so.

## 9.4 Second uncomfortable exchange

Line 11:

Speaker: `Cashier`

Text:

"You should get home soon."

Line 12:

Speaker: `You`

Text:

"Why? Is something happening?"

Line 13:

Speaker: `Cashier`

Text:

"Stores close. Streets get empty. People get lost."

Line 14:

Speaker: `You`

Text:

"That's... a weird thing to say."

After Line 14, wait approximately 1.0 second before the final cashier line becomes available.

The player should remain able to read the final line normally once it appears.

## 9.5 Final line

Line 15:

Speaker: `Cashier`

Text:

"Have a pleasant night."

The cashier's final line should initially sound polite and ordinary.

After the player has finished reading it and clicks to end the conversation:

1. Hide the dialogue panel.
2. Prevent ordinary interactions from restarting the conversation.
3. Keep player movement locked for the short scripted event.
4. Wait exactly the configured pre-transformation delay, default 1.5 seconds.
5. Trigger the transformation sequence.

The default delay is 1.5 seconds, but it must be editable.

Do not transform the cashier while the final line is still being typed or while the player has not yet completed that line.

## 9.6 Transformation sequence

Create a configurable event sequence for the cashier transformation.

Default sequence:

**Phase 1 — Silence**
- Duration: 0.5 seconds.
- Reduce or stop the normal store ambience if the assigned audio controls permit it.
- Do not immediately play a loud jumpscare sound.
- Keep the player from moving during the sequence.

**Phase 2 — Visual pause**
- Duration: 0.75 seconds.
- The cashier stops ordinary movement.
- If an appropriate Animator is assigned, trigger an unsettling idle or head movement.
- If no Animator is assigned, skip the animation gracefully.

**Phase 3 — Transformation**
- Trigger the assigned monster animation, model swap, or configured visual event.
- Default transformation event duration: 2.0 seconds.
- Play the assigned transformation sound if available.
- Optional camera movement and screen effects may be configured later, but they must not be required for the sequence to function.

**Phase 4 — Finish**
- Invoke the configured transformation-complete event.
- Restore player movement according to the configured outcome.
- Advance to the next story state.

The transformation must be triggered exactly once.

Do not attempt to transform a nonexistent model or Animator.

Provide useful warnings when required references have not been assigned.

Do not create a monster model, animation, sound, or asset path that does not exist.

Use placeholders or UnityEvents where needed so the developer can assign their own assets.

## 9.7 Successful transformation outcome

The successful shopping path requires a brief, unsettling cashier transformation.

Support a configurable option to restore the cashier's normal appearance after the transformation.

Default for the initial implementation: enable the temporary transformation and restore the original cashier state after the configured sequence finishes.

This is a development default, not a permanent story decision.

The developer must be able to disable the restoration and use a different ending later.

After the transformation finishes:
- Complete checkout once.
- Record that the player successfully completed this shopping session.
- Expose a clear event for the future story sequence.
- Do not automatically return to the apartment or implement the later car incident in this task.
- Leave the project ready for the developer to add the next story beat.

---

# PART 10 — INCOMPLETE SHOPPING AND FAILURE PATH

The game must distinguish between successful checkout and an incomplete shopping list.

The player should not receive the successful checkout outcome if required groceries are missing.

## 10.1 Incomplete checkout trigger

When the player attempts checkout with missing items, show a separate conversation ID:

`CASHIER_Incomplete_01`

Use this dialogue:

Line 1:

Speaker: `Cashier`

Text:

"Hold on. I think you're missing something."

Line 2:

Speaker: `You`

Text:

"What? I got everything."

Line 3:

Speaker: `Cashier`

Text:

"Not everything on your list."

Line 4:

Speaker: `You`

Text:

"Are you sure?"

Line 5:

Speaker: `Cashier`

Text:

"Would you like to check again?"

After the conversation ends, return control to the player and show the incomplete shopping list.

Do not complete checkout.

The player should be able to return to the shelves and collect the missing items.

## 10.2 More intense failure outcome

The developer wants an incorrect or incomplete shopping result to produce a more intense outcome than the successful path.

Implement this as a configurable failure event, not a hardcoded, irreversible game-over state.

Suggested initial behavior:
- The cashier becomes visibly more unsettling.
- The ambient sound distorts briefly.
- A stronger sound effect or visual event plays if assigned.
- The interaction ends without marking checkout as complete.
- The player can recover and complete the list.

The developer must be able to change this behavior later.

Do not permanently trap the player or make the game unwinnable simply because they attempted to check out too early.

If the developer later chooses a true failure ending, that ending should be configured explicitly.

---

# PART 11 — MISSION LOG AND OBJECTIVE MANAGER

## 11.1 Existing systems

Reuse `ObjectiveManager.cs` and `QuestLogUI.cs`.

Do not create duplicate objective lists or mission logs.

The mission log should read current state from the authoritative objective and shopping systems.

If separate UI panels are required, make sure they are managed by the existing UI architecture rather than spawned repeatedly.

## 11.2 Required objectives

Create the following initial objective sequence:

**Objective 1**

`Turn off all the lights in the house`

Completion condition: all required configured house light switches are off.

**Objective 2**

`Go outside to the car`

Completion condition: successful interaction with the designated apartment exit or its associated successful scene transition.

**Objective 3**

`Drive to the grocery store`

Completion condition: successful arrival at the configured grocery-store scene through the intended trip.

**Objective 4**

`Collect the required groceries`

Completion condition: every configured shopping item and quantity has been collected.

**Objective 5**

`Pay for the groceries`

Completion condition: successful cashier checkout after the required items have been collected and the transformation sequence has finished.

The exact objective ID should remain stable even if the visible description changes.

Do not use the description text as the unique identifier.

## 11.3 Objective UI behavior

Use the existing top-left objective HUD.

Display the active objective clearly.

When it completes:
- Mark the objective complete once.
- Update the mission log.
- Briefly display a completion notification.
- Activate the next objective according to the specified story sequence.

Do not show several contradictory objectives as active simultaneously unless the developer deliberately configures parallel objectives.

If an objective is already complete, scene re-entry must not reset it without an explicit session-reset action.

## 11.4 Mission-log behavior

M toggles the mission log.

Default behavior:
- Press M once to open.
- Press M again to close.
- Ignore repeated key events caused by holding M.
- Do not create additional UI objects.
- Refresh the display using current objective and shopping state.
- Preserve shopping progress when the window is closed.

On entering the configured grocery-store scene, queue the automatic shopping-list display if configured.

If a dialogue is active, do not allow the mission log to cover the dialogue and create an unreadable overlap.

Show the mission log after the initial store dialogue finishes.

The developer must be able to configure which scene triggers automatic display.

---

# PART 12 — INTERACTION COORDINATION

Several systems use the same keys. This must be handled deliberately.

## 12.1 F-key priority

The F key is used for grocery pickup and object inspection.

When F is pressed:
1. Determine whether a modal system such as dialogue owns input.
2. If dialogue owns input, do nothing except advance dialogue using its designated input.
3. If inspection is active, send the input to the inspection system as intended.
4. Otherwise, resolve the current valid interaction target.
5. Execute only that target's action.
6. Prevent other nearby objects from also responding to the same keypress.

Do not allow an object behind the intended target to collect an item accidentally.

If multiple targets overlap, use a predictable selection rule, such as nearest valid target or existing interaction priority.

Expose interaction priority in the existing architecture if necessary.

## 12.2 E-key priority

E is used for interactions such as the car or doors.

When E is pressed:
- Resolve one valid interaction target.
- Execute only one interaction.
- Ignore additional E events while a transition is in progress.
- Prevent interaction triggers from starting the same scene transition repeatedly.

## 12.3 Left-click priority

While dialogue is active, left-click belongs to the dialogue system.

Do not let it also interact with world objects.

After dialogue ends, release input ownership normally.

Do not accidentally leave the player unable to interact after a conversation.

## 12.4 Interaction prompts

Prompts should:
- Appear only when the target is in range.
- Disappear when the target is out of range.
- Disappear when the target becomes unavailable or is collected.
- Avoid overlapping one another.
- Reuse UI objects where practical.
- Use configurable key names and display text.

---

# PART 13 — CONFIGURATION REQUIREMENTS

The developer will change this story frequently.

Where appropriate, expose the following as Inspector fields or editable configuration assets.

## Dialogue

- Dialogue sequence or asset.
- Speaker names.
- Dialogue lines.
- Line order.
- Typewriter speed.
- Dialogue panel appearance.
- Font and font size.
- Voice clips.
- Sound effects.
- Delays before lines.
- Optional post-line pauses.
- Player-movement lock.
- Optional conversation-enabled setting.
- Conversation completion event.

## Objectives

- Stable objective ID.
- Display description.
- Completion event or condition.
- Completion notification duration.
- Next objective.
- Optional dialogue on activation or completion.

## Scene settings

- Apartment scene.
- City/exterior scene.
- Grocery-store scene.
- Automatic mission-log scene.
- Optional scene-transition event.
- Optional scene-readiness delay.

## Grocery system

- Item identifiers.
- Item display names.
- Required quantities.
- Grocery model references.
- Pickup sounds.
- Interaction radius.
- Interaction key.
- Shopping-list display settings.
- Collection progress.
- Missing-item behavior.

## Cashier

- Cashier interaction key.
- Cashier interaction radius.
- Successful checkout dialogue.
- Incomplete checkout dialogue.
- Scanner sounds.
- Normal ambience.
- Transformation audio.
- Transformation animations or UnityEvents.
- Transformation timing.
- Restore-after-transformation setting.
- Successful checkout event.
- Failure-path event.

Do not put every setting into one enormous, difficult-to-use component if that would make the Inspector confusing.

Organize related fields with clear headers and descriptive tooltips.

Use reasonable defaults and explain which references the developer must assign manually.

---

# PART 14 — ERROR HANDLING AND SCENE LIFECYCLE

## 14.1 Missing references

If an optional voice clip, sound, animation, icon, or visual effect is missing:
- Skip that optional feature.
- Continue the main event.
- Do not throw a null-reference exception.

If a required reference such as a player, scene configuration, or required objective dependency is missing:
- Provide a clear warning identifying the missing component.
- Do not silently mark an objective complete.
- Avoid leaving the player's controls permanently locked.

## 14.2 Scene changes

Before modifying scene persistence, inspect the existing system.

Ensure:
- Dialogue does not continue with stale player references after a scene change.
- The mission log is not duplicated.
- Objectives are not duplicated.
- Shopping progress follows the intended lifetime.
- Event subscriptions are registered and removed properly.
- Interactions from the previous scene are no longer active.
- A scene transition cannot be triggered multiple times by holding a key.

Do not reset shopping progress whenever the mission log closes.

Do not reset completed objectives whenever the player enters another scene.

Provide an explicit reset for testing a new playthrough.

## 14.3 Restart and replay

The developer must be able to restart the sequence during testing.

A deliberate session reset should:
- Reset the opening sequence.
- Reset objective progress.
- Reset the shopping cart.
- Reset collected grocery states where appropriate.
- Reset checkout completion.
- Reset cashier-transformation state.
- Restore configured actors to their original states.

Do not reset the entire game simply because the player reopens a menu.

---

# PART 15 — IMPLEMENTATION ORDER

Do not attempt to rewrite every script simultaneously.

Use this implementation order.

**Stage 1: Audit**
- Read the current scripts.
- Identify what already works.
- Identify missing functionality.
- Report proposed modifications before making broad architectural changes.

**Stage 2: Dialogue**
- Verify the UI helper.
- Verify dialogue creation.
- Verify typing and click progression.
- Verify conversation locking.
- Add configurable dialogue data.
- Test the apartment opening conversation.

**Stage 3: Objectives and lights**
- Integrate the opening dialogue with objective 1.
- Verify all required lights are tracked correctly.
- Verify objective 2 activates after successful completion.

**Stage 4: Car and scene transition**
- Add or integrate the car interaction.
- Activate objective 3 at the correct time.
- Verify arrival at the configured grocery-store scene.

**Stage 5: Grocery list**
- Add configurable shopping requirements.
- Verify interaction with grocery models.
- Verify collection counts.
- Verify the mission log.
- Verify M-key opening and closing.
- Verify scene-entry automatic opening.

**Stage 6: Cashier**
- Add the successful checkout conversation.
- Add the incomplete-checkout dialogue.
- Add the transformation event.
- Verify that it only occurs at the correct point.
- Verify the success and failure paths.

**Stage 7: Integration testing**
- Test the complete intended sequence from a fresh session.
- Fix duplicate input handling, stale references, objective-order problems, and UI conflicts.
- Confirm that the systems remain configurable.

Avoid rewriting working scripts simply to match the structure suggested in this document.

If the current architecture differs from this specification, preserve the working architecture and implement equivalent behavior through the appropriate existing systems.

---

# PART 16 — ACCEPTANCE TESTS

Do not report the feature as complete solely because the scripts compile.

Test the gameplay behavior.

## Test A: Apartment opening

1. Start a fresh session in the apartment.
2. Wait for the configured initial delay.
3. Verify the first dialogue appears.
4. Click once while text is typing.
5. Verify the text finishes but does not immediately advance.
6. Click again.
7. Verify the next line appears.
8. Finish the conversation.
9. Verify objective 1 activates exactly once.

## Test B: Lighting objective

1. Turn off only one required light.
2. Verify the objective remains active if other required lights are on.
3. Turn off the remaining required lights.
4. Verify objective 1 completes exactly once.
5. Verify objective 2 activates.
6. Verify decorative lights do not interfere unless configured as required lights.

## Test C: Car

1. Enter the configured exterior scene.
2. Approach the car.
3. Verify the interaction prompt appears.
4. Move away.
5. Verify the prompt disappears.
6. Return and press E.
7. Verify the car dialogue starts once.
8. Finish the dialogue.
9. Verify objective 3 activates.
10. Verify no duplicate transition starts.

## Test D: Grocery-store arrival

1. Enter the designated store scene through the intended route.
2. Verify objective 3 completes.
3. Verify the opening dialogue appears.
4. Finish the dialogue.
5. Verify the instruction appears for the configured duration.
6. Verify the mission log opens automatically.
7. Close it using M.
8. Open it again using M.
9. Verify the UI is not duplicated.

## Test E: Grocery collection

1. Approach a required item.
2. Verify the F interaction prompt appears.
3. Press F once.
4. Verify one item is collected.
5. Verify the progress updates immediately.
6. Press F repeatedly.
7. Verify the same unique object cannot be collected repeatedly.
8. Collect the remaining requirements.
9. Verify the completion dialogue plays no more than once.
10. Close and reopen the shopping list.
11. Verify the collection progress is preserved.

## Test F: Incomplete checkout

1. Leave at least one required item uncollected.
2. Approach the cashier.
3. Press E.
4. Verify the incomplete-checkout conversation starts.
5. Finish the conversation.
6. Verify checkout is not marked complete.
7. Verify the player can collect missing items.
8. Verify no permanent soft lock occurs.

## Test G: Successful checkout

1. Collect every required item and quantity.
2. Approach the cashier.
3. Press E.
4. Verify the successful dialogue begins.
5. Confirm the lines appear in the specified order.
6. Confirm the scanner sequence occurs after the specified line.
7. Confirm the uncomfortable dialogue pauses occur.
8. Confirm the final line is fully displayed.
9. Finish the conversation.
10. Verify the configured transformation delay.
11. Verify the transformation sequence plays once.
12. Verify the success event fires once.
13. Verify the player controls are restored according to configuration.

## Test H: Robustness

1. Remove an optional sound clip.
2. Verify the event still works.
3. Remove an optional animation reference.
4. Verify the event still completes.
5. Change a dialogue line in the Inspector.
6. Verify the new text appears without code changes.
7. Change the grocery item display name.
8. Verify stable item matching still works.
9. Change the configured store scene.
10. Verify scene detection uses the new configuration.
11. Restart the session.
12. Verify dialogue, objectives, shopping state, and cashier state reset correctly.
13. Verify no duplicate UI managers or input handlers appear.

---

# PART 17 — FINAL REPORT REQUIRED

When finished, provide a concise implementation report with these headings:

**Existing scripts inspected**

List the relevant scripts you actually opened and reviewed.

**Scripts changed or created**

List each modified or created file and explain its responsibility.

**Inspector setup required**

List the exact references, scene names, dialogue assets, sounds, animations, and settings the developer must assign manually.

**Behavior implemented**

Describe the features that actually work.

**Testing performed**

List the acceptance tests that were actually executed. Do not claim a test passed if it was not run.

**Remaining limitations**

Explain anything incomplete, any asset that is still missing, and any manual setup that remains.

Do not claim that the full sequence works merely because the project compiles.

The final objective is a configurable, maintainable story system for Paralysis that the developer can continue expanding without rewriting the entire project.