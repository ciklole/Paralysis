# Spec written by the developer (Serali / ciklole1)

Sent to the AI agent on 2026-11-04 04:37 UTC, 12680 characters, in the chat session.
Reproduced here verbatim from the session history.
---

# TASK: FULL CASHIER TRANSFORMATION SEQUENCE — LIGHTING, ANIMATIONS, MONSTER AI & CAMERA CONTROL

You have permission to modify the store map and any relevant systems, scripts, animations, lighting configurations, and other assets necessary to complete this task.

I want you to significantly improve the cashier interaction and transformation sequence. This should feel like a professionally developed indie horror game, with carefully timed animations, atmospheric lighting, convincing character movement, and cinematic camera behavior.

**IMPORTANT: Do not treat this as a quick implementation. Take the time necessary to investigate the existing project, understand its architecture, implement the complete experience, test it thoroughly, and polish every detail.**

---

## 1. STORE LIGHTING — COMPLETE RED LIGHT TRANSFORMATION

When the cashier transforms into the monster, I want EVERY light source in the entire store scene to turn red.

This means ALL light sources, not just the obvious ones.

Requirements:

- Identify every light source in the store scene, including point lights, spotlights, directional lights, area-light equivalents, emissive lighting setups, and any other systems contributing to the store's illumination.
- When the transformation begins, smoothly transition all applicable light sources to a frightening red color.
- The transition should feel cinematic rather than instantaneous.
- The red lighting should affect the entire store and create a disturbing atmosphere.
- When the transformation sequence finishes or the appropriate event occurs, smoothly restore ALL lighting to its original appearance.

**Extremely important:**
- Remember the original color, intensity, range, spot angle, and other relevant properties of every modified light.
- Restore the exact original configurations afterward.
- Do not permanently overwrite the original lighting settings.
- Prevent lights from being modified multiple times accidentally.
- Ensure that lights controlled by existing systems continue working correctly after the sequence.
- Avoid creating duplicate lighting controllers if an existing system can be extended safely.

If emissive materials or other illumination effects are used in the store, investigate whether they need to participate in the transformation as well.

I want the entire store to feel like its atmosphere has suddenly changed.

## 2. CASHIER ANIMATIONS — NATURAL IDLE AND DIALOGUE MOVEMENTS

The cashier currently needs significantly more life and personality.

I want the cashier to have a proper animation system with several animation states.

### A. Idle animation

Create a subtle, continuously looping idle animation.

The cashier should:
- Slightly bob up and down.
- Have natural body movement.
- Avoid looking completely static.
- Return smoothly to the correct idle pose after other animations.

The movement should be subtle. Do not make the cashier look like they are constantly bouncing.

### B. Dialogue animations

Whenever the player asks a question or the cashier gives an answer, the cashier should perform an appropriate animation.

Examples:
- Slightly moving their head while responding.
- Small hand gestures.
- Subtle body movements.
- Brief changes in posture.
- Other natural movements appropriate for a cashier having a conversation.

Requirements:

- Each dialogue interaction should trigger an appropriate animation.
- Animations should not look identical every single time if multiple suitable animations are available.
- The cashier must smoothly transition back into their idle animation after each dialogue animation.
- Avoid animation interruptions, overlapping animations, or unnatural snapping.
- Ensure animations synchronize properly with the existing dialogue system.

Do not unnecessarily modify the dialogue itself. Preserve all existing dialogue, dialogue progression, and interaction functionality.

## 3. MONSTER TRANSFORMATION — CASHIER BECOMES A THREAT

When the cashier transforms into the monster, I want the transformation to feel genuinely frightening.

The monster should no longer behave like an ordinary cashier.

### A. Transformation

Create a convincing transition between the normal cashier and monster states.

Requirements:
- The transformation should be properly synchronized with the red lighting sequence.
- The monster should have its own animation state.
- The transition should not look like a simple model swap unless the existing project architecture requires it.
- Preserve the existing monster design and character assets wherever possible.
- Make the transformation feel intentional and cinematic.

### B. Monster movement

After transforming, the monster should begin moving toward the player.

Requirements:
- The monster should gradually approach the player's position.
- Its movement should feel threatening and deliberate.
- The monster should face the player appropriately.
- Avoid unnatural sliding, teleportation, or sudden position changes.
- Ensure its movement works correctly regardless of the player's position within the interaction area.

Do not let the monster move through objects or store geometry unnecessarily.

### C. Monster reaching animation

This is particularly important.

I want the monster to eventually reach toward the camera with BOTH hands.

The animation should communicate that the monster is trying to grab the player.

Requirements:
- The monster should extend its arms toward the player/camera.
- The movement should be properly animated, not just achieved by rotating the entire character.
- Both hands should move naturally.
- The animation should have appropriate anticipation and follow-through.
- The monster should hold its reaching pose briefly before transitioning to the next event.
- Make sure the hands are oriented toward the camera rather than pointing in random directions.

Use the existing character rig and animation assets where possible. If necessary, create additional animations or procedural animation support.

The final result should feel like the monster is reaching directly out of the screen.

## 4. CAMERA CONTROL — LOCK THE CAMERA DURING CASHIER INTERACTION

I want the cashier interaction to use the same camera-locking behavior that we already implemented for interactable objects in the apartment.

When the player starts speaking to the cashier:

1. The camera should smoothly move or transition into the designated interaction position.
2. The camera should lock in place.
3. The player should no longer be able to freely rotate the camera.
4. The camera should remain locked throughout the dialogue interaction.
5. When the interaction finishes, restore the appropriate camera control.

**IMPORTANT:** Investigate the existing object-interaction camera system first.

I specifically want you to reuse the same architecture and behavior wherever possible instead of implementing an entirely separate camera-locking system.

Additional requirements:
- Preserve the existing camera sensitivity settings.
- Do not break the first-person controller.
- Avoid conflicts with mouse locking and cursor visibility.
- Ensure the camera does not jitter or move unexpectedly.
- Make sure the camera remains properly positioned during the monster transformation.
- The monster's reaching animation must be aligned with the player's camera position.

The camera should behave as though the player is watching a carefully directed horror cutscene.

## 5. SEQUENCE SYNCHRONIZATION

All of these systems must work together.

I do NOT want several independent systems triggering randomly or fighting each other.

Create a clear sequence that coordinates:

- Existing cashier dialogue.
- Cashier idle animations.
- Dialogue-specific animations.
- Monster transformation.
- Store-wide red lighting.
- Camera locking.
- Monster movement.
- Monster reaching animation.
- Existing success/failure and game-over logic.

The exact timing should be determined by investigating the existing dialogue and transformation systems.

Do not break the existing grocery mini-game or its dialogue conditions.

In particular:
- Preserve the existing behavior when the player has the correct grocery list.
- Preserve the existing behavior when the player has an incorrect grocery list.
- Do not accidentally trigger the monster sequence more than once.
- Ensure all temporary camera, lighting, animation, and movement states are cleaned up correctly when the sequence ends.
- Handle scene transitions and unexpected interruptions safely.

## 6. CODE QUALITY AND PROJECT ARCHITECTURE

Before implementing anything, inspect the existing project thoroughly.

Identify:
- The store scene and its lighting hierarchy.
- The cashier GameObject and character rig.
- Existing animation controllers and available animation clips.
- The cashier dialogue system.
- The monster transformation logic.
- Existing camera interaction scripts.
- The first-person controller.
- Existing scene transition and game-over systems.

Follow the existing project's architecture and naming conventions.

Avoid:
- Unnecessary duplicate scripts.
- Multiple competing animation controllers.
- Hardcoded references when serialized references would be more appropriate.
- Replacing existing working systems without a good reason.
- Breaking serialized Unity references.
- Modifying unrelated scenes or systems.
- Leaving unfinished placeholder implementations.

If an existing system can be extended safely, extend it rather than rebuilding it.

## 7. TESTING AND QUALITY ASSURANCE

Do not consider the task complete simply because the code compiles.

You must thoroughly test the actual experience in Unity.

Test at least the following scenarios:

**Scenario A — Correct grocery list**
- Complete the grocery mini-game correctly.
- Approach the cashier.
- Start the dialogue.
- Verify that dialogue animations play.
- Verify the idle animation works between dialogue interactions.
- Trigger the transformation.
- Verify every applicable store light turns red.
- Verify the camera locks correctly.
- Verify the monster approaches the player.
- Verify the monster reaches toward the camera with both hands.
- Verify the existing success/game-over progression remains correct.

**Scenario B — Incorrect grocery list**
- Complete the grocery mini-game incorrectly.
- Verify the existing incorrect-list behavior remains intact.
- Verify the intended transformation and game-over sequence still works.

**Scenario C — Restoration and cleanup**
- Verify lighting returns to its exact original settings when appropriate.
- Verify the camera unlocks when appropriate.
- Verify the player's controls are restored.
- Verify no animation or movement states remain stuck.
- Verify no errors appear in the Unity Console.

**Scenario D — Repeated testing**
- Repeat the interactions multiple times.
- Look for duplicated animations, repeated lighting changes, broken references, and unexpected state transitions.
- Test scene transitions and interruptions where applicable.

Fix every issue you discover.

---

## 8. FINAL POLISH — DO NOT RUSH

Once all functionality is implemented, review the entire sequence from the player's perspective.

Ask yourself:

- Does the cashier feel alive before transforming?
- Do the dialogue animations look natural?
- Does the red lighting genuinely change the atmosphere of the entire store?
- Does the camera feel intentionally directed?
- Does the monster's movement look convincing?
- Does the monster reaching toward the camera create a strong horror moment?
- Are the transitions smooth?
- Does everything feel like one cohesive cinematic experience?

If something looks unfinished, improve it.

Do not sacrifice quality just to finish quickly.

## 9. FINAL REPORT

When everything is finished, provide a detailed report containing:

1. Every script created or modified.
2. Every animation created, reused, or modified.
3. Every lighting configuration changed.
4. How the cashier and monster animation systems work.
5. How the camera-locking system was integrated.
6. How the different sequences are synchronized.
7. Testing performed and its results.
8. Any remaining limitations or issues.

**FINAL INSTRUCTION:**

You have considerable creative freedom to improve the presentation, animation quality, and horror atmosphere, provided you preserve the existing gameplay, story, and project architecture.

Work methodically. Investigate before modifying, implement before polishing, and test before declaring completion.

I want a complete, polished, playable cashier transformation sequence — not merely a collection of scripts that theoretically work.

yeah. and dont forget to compact every time u see the tokens are low