Paralysis - who did what

This is the honest breakdown of who did what on this game, written because
people asked how much of it was AI. The short version: I made the game, and
an AI agent did a lot of the heavy lifting on the code and on the tooling
that built the UI, the menus and the cashier sequence. I am not going to
pretend otherwise, and I am not going to claim a fake percentage either.

The full machine-generated record of every command the agent sent into the
Unity editor is in `Docs/AIReview/`. Start there if you want to check me.

What I wrote, by hand

- The story, the characters, the three chapters, and the ending.
- The design brief. Over the whole session I typed 108 messages, 163,364
  characters in total, and 8 of those were long specification documents
  (139,643 characters) that are the actual design of the game in my own
  words. They are copied verbatim into `Docs/AIReview/specs/`: the story,
  dialogue, objective and interaction spec from Oct 24 (46,545 chars), the
  main menu and UI spec from Oct 26 (38,173 chars, 12 numbered
  deliverables), the cashier transformation spec from Nov 4 (12,680 chars)
  and the final polish spec from Nov 10 (29,746 chars).
- The dialogue. All 9 conversation assets in `Assets/Dialogue/` and the 115
  lines in them. The agent never wrote or edited a single one of those
  files. The words are in the Oct 24 spec, and the agent transcribed them
  into the assets. For example the spec says, for `STORE_Opening_01`:
  Line 1 "Alright. Let's see what we need.", Line 2 "Bread, milk, eggs...
  should be easy enough." That is character for character what
  `Assets/Dialogue/STORE_Opening_01.asset` contains.
- The apartment. `Assets/Scenes/Home.unity` was never touched by the agent.
  I built that scene myself.
- The ending scene. `Assets/Scenes/End.unity` was never touched either. The
  agent's end-screen builder literally logged `End scene already exists;
  left untouched.`
- The driving scene, to start with. I built `CarDriving.unity` and the car
  handling before the agent got involved with it.
- The main menu scene, to start with, and the art direction for the whole
  game: which props, which lighting, which mood.
- Every third-party art, music and sound pack choice and placement. I did
  not make those assets, I picked them and put them where I wanted them.
- The city. I built it. The roads, the crossroads, the sidewalks, the racing
  car, the bench, all the street lights and most of the buildings were placed
  by me in the editor, and they are all still there. Every one of the 129
  street lights in the final scene is one I placed by hand. The last 80 of
  them are named `V Cone LiT (48)` through `V Cone LiT (127)`, which is the
  naming Unity gives when you duplicate an object in the editor, and they sit
  in pairs on both sides of the pavement at irregular intervals rather than in
  a grid.
- The agent's attempt to build the city. On Nov 5 I asked it to fill in the
  rest of the street with vrbn houses ("okay, can u build a city? ... i
  already put some of them ... and you are free to use my editor to do
  so"). Its generator placed 19 house prefabs, using the wrong models, so
  on Nov 7 I had to send it a screenshot of the actual 3D building
  prefabs before it would use the LOD meshes. On Nov 10 it ran
  `DrivingSceneSyncTool.ApplyReverseSyncBatch`, which copied the CarDriving
  scene's environment and lighting into City so the two would match. That
  copy brought in 16 buildings and 6 lights that I had placed by hand in
  the driving scene.
- The rest of the city work. After the Nov 10 copy, I went back into the
  City editor myself and added 80 more street lights and 6 more buildings,
  then had the agent sync City into CarDriving so the two still matched.
  That is the whole city story: the generator's 19 houses, and a lot of
  copying from one scene into the other. The layout is mine.
- The store scene was regenerated once by the agent's `StoreSceneBuilder`
  on Oct 25, and I hand edited the lighting and the prefab placements
  afterwards. The uncommitted diff in my working tree is my manual pass
  over it.
- The design decisions: what the shopping list is, how the cashier
  sequence works, what the monster does, when the scare happens, what the
  ending says.

What the AI agent did

The agent was OpenCode, running on my machine from Oct 10 to Nov 23. It had
two ways into Unity, and both are logged.

- It sent 158 commands through the request-file dispatcher
  (`Temp/urp_request.txt`, read by `Assets/Editor/UrpMaterialTools.cs`),
  across 27 distinct commands: 31 test-suite runs, 24 play-state checks,
  16 main-menu rebuilds, 13 pumpkin reimports, 10 animation dumps, 9
  cashier-sequence rebuilds, 9 cashier screenshots, and so on.
- It launched Unity headless 54 times with `-batchmode -nographics
  -executeMethod`, including 14 runs of `CityBuilder.BuildCity`, 15 runs of
  `SmokeTestRunner.Run`, 3 runs of `AuthoredUIBuilder.BuildAllBatch`, and
  the Windows player builds.
- It wrote or edited 110 of the 111 C# files in the project, including all 21
  editor tools in `Assets/Editor/`: `CityBuilder`, `StoreSceneBuilder`,
  `MainMenuSceneBuilder`, `AuthoredUIBuilder`, `EndScreenBuilder`,
  `CashierSequenceBuilder`, `DrivingSceneSyncTool`, `SmokeTestRunner`,
  `UrpMaterialTools`. For 106 of those files the committed version is the
  agent's own writing; the other 4 have agent edits that are still sitting
  uncommitted in my working tree.
- It patched `CarDriving.unity` and `MainMenu.unity`, and used its
  `DrivingSceneSyncTool` to copy the environment back and forth between the
  city and the driving scene so the two would look the same.
- It wrote the docs in `Docs/`, packaged the Windows build, and wrote the
  acceptance suite the releases were checked against.
- It explained errors when I got stuck, which is the job I originally gave
  it, and that part was genuinely useful.

The one runtime script it never touched is
`Assets/Scripts/UI/ButtonAppearance.cs`.

What the 40 hours actually measures

I tracked my editor time with Hackatime: 40h 41m total for the Paralysis
project, between Oct 10 and Nov 14. That number is real but it is not a
measure of authorship, and I want to be careful about how I use it.

- 35h 11m of it (88.7%) was logged before Nov 5, which is the day the big
  city generation runs started.
- On Nov 5, 6 and 7, the agent ran the city builder 14 times in headless
  mode, and my tracked time those three days was 10, 14 and 16 minutes.
- Headless Unity runs do not count as editor time, so a lot of the agent's
  work is invisible to the tracker. Some of the tracked time is me sitting
  in the editor while the agent drives it, and some of it is me hand
  editing. The tracker cannot tell those apart.
- So the honest reading is: most of the tracked time is me in the editor,
  but it is not all hand-authoring, and I cannot honestly call all of it
  mine.

What the git history proves, and what it does not

23 commits, all under my name and email, across 7 active days between
Oct 11 and Nov 11. The full project landed in one bulk commit on Nov 4
(3,971 files), which is why the commit count is low.

What that proves: I was the one making every commit, deciding what shipped
and when, and I reviewed and accepted the agent's output before it went in.
The commit messages are mine.

What that does not prove: git records who committed, not who typed. It
cannot tell you a line of code apart from a line of scene YAML if both went
in the same commit. So the commits are evidence of ownership of the
project and the releases, not a proof of authorship of every file.

The part I can actually prove is narrower and stronger: the agent's tool
history records every file it wrote or edited. Cross-checking that against
the tracked files gives me the list of files it never touched, and those
are the ones I wrote by hand. That list includes all 9 dialogue assets,
`Home.unity`, `End.unity`, and `ButtonAppearance.cs`.

The city scene is the clearest case. It was mine before the agent ever
touched it, and the scene file's history says so. At `63f8b1f` (Nov 4,
the bulk upload) the city already had 110 prefab instances in it: 40 road
pieces, 43 street lights, 24 vrbn buildings, the racing car and the bench.
I added 14 more buildings by hand at `327d543` the same day. The agent's
contribution was 19 house prefabs on Nov 5 with the wrong models, the
corrected LOD versions on Nov 7 after I sent it a screenshot, and copies
between the two scenes on Nov 10. The full per-commit table with the
counts is in `Docs/AIReview/README.md`, the raw counts are in
`Docs/AIReview/receipts/city_commit_counts.txt`, and any row is checkable
with `git show <commit>:Assets/Scenes/City.unity`.

What the repo contains

- 6 scenes: MainMenu, Home, City, CarDriving, Store, End.
- 653 GameObjects across those scenes: MainMenu 53, Home 249, City 87, CarDriving 94, Store 159, End 11.
- 111 C# files, 32,287 lines total.
  - 90 runtime scripts, 22,018 lines.
  - 21 editor tools, 10,269 lines.
- 9 dialogue conversation assets and 115 dialogue lines across the project.
- 22 materials in the shared materials folder.
- 14 game data and UI assets in Resources/Paralysis.
- 163 files across the scripts, dialogue, scenes, materials, and game data folders, not counting .meta files.
- 23 git commits, all under the same email, over 7 active days between Oct 11 and Nov 11, 2026.

How to check

From the repo root:

    git log --oneline
    git shortlog -sn
    Get-ChildItem Assets\Scripts,Assets\code,Assets\Editor -Recurse -Filter *.cs

To count GameObjects in a scene, search the .unity file for lines starting
with "--- !u!1 &".

To see every command the agent sent into the editor:

    Docs\AIReview\editor_commands.csv

To read the summary of the same thing:

    Docs\AIReview\README.md

To read my own design specifications, which the agent implemented:

    Docs\AIReview\specs\

The commit history is also on GitHub:
https://github.com/ciklole/Paralysis/commits/main
