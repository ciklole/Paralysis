PARALYSIS

Paralysis game is a short first-person horror story(it is not scary 🥀) about a late-night grocery run that should have been normal. You wake up in your apartment, turn off the lights, drive through an empty city, and walk into a grocery store that feels more wrong the longer you stay. It has three chapters, dialogue, driving, shopping, a checkout scene, and an ending. It's a complete little demo, not a full-length game.

This is my first horror game and my first storytelling game. It's also one of the biggest projects I've made so far. It's not really scary, though. It's pretty lightweight and goofy more than anything.

I built it in Unity 6.7 alpha over a month and a half. The story, the characters, the dialogue, the chapter structure and every gameplay decision are mine. The apartment and the ending scene are mine too. I. Placed the third-party props, cars and environments myself. I also used AI as a working partner. It did a lot more than debug. It wrote most of the code in this repo. It wrote the editor tooling. It generated the city scene for me. I asked for that, I reviewed it and I committed it. It also packaged the Windows build. Helped with the README wording.

It was hard. I ran into a lot of bugs. The worst one was a rendering bug when I moved the project from the built-in render pipeline to URP. I had to go through every material and fix it by hand, which took forever. My laptop also kept choking on the scenes. Turns out my graphics drivers were ancient. Updating them helped enough that the scenes would actually load, though performance still is not amazing. If the game runs rough on your machine, try updating your drivers

About AI, honestly, because people asked and I would rather say it plainly than dodge it. The debugging help was real. When Unity threw an error I did not understand, I pasted the error and the relevant code into an AI chat. Used it to explain what went wrong. Sometimes it suggested a fix. I tested it. That was the half of what AI did on this project though. An AI agent ran on my machine for the build. It. Edited 110 of the 111 C# files, including every editor tool in Assets/Editor. It generated the city scene with a generator. Rebuilt it fourteen times. It did the UI canvases, the menu assets, the end screen wiring and the cashier sequence. The story, the words, the apartment, the ending and the design are mine. The code and the city are the agents, with me directing it, reading what it made and deciding what ships.

I would rather show you than tell you. Docs/AIReview/editor_commands.csv has every single command the agent sent into my Unity editor, with timestamps and Docs/AIReview/README.md explains what each one did. That is 212 editor invocations in total, 158 through the request-file dispatcher and 54 headless Unity runs. Docs/AIReview/specs/ has the design documents I wrote for the agent, including the story and dialogue spec that the dialogue in the game comes from word for word.

I tracked 40 hours in the Unity editor with Hackatime. I want to be careful with that number too. It is the time the editor was open on my machine, which's not the same as "time I typed by hand". Some of it is me hand editing scenes. Some of it is me sitting there while the agent drove the editor. The headless runs do not show up in it all. So please do not read the 40 hours as a claim that every minute was mine.

I am not going to put a percentage on it. Here is what the project actually contains, so you can judge the work yourself:

- 6 scenes: MainMenu, Home, City, CarDriving, Store, End.

- 653 GameObjects across those scenes.

- 111 C# files, about 32,000 lines. 90 Runtime scripts (~22,000 lines) and 21 editor tools (~10,000 lines).

- 9 dialogue conversation assets and 115 dialogue lines in the project.

- 22 materials and 14 game data/UI assets in the folders I built.

- 23 git commits, all mine, over 7 days between Oct 11 and Nov 11, 2026.

The commit count is low because the full project was uploaded in one batch on Nov 4, not because I only worked 7 days. The 1.5 months is the timeline. You can check the scenes, scripts, materials. Commit history in this repo. The full breakdown and the commands to check it are in evidence.md.

Sorry, but there is no web demo as the file is too big.

Download the game it talks thoroughly about how to download it:

https://github.com/ciklole/Paralysis/releases/tag/v0.67

Oh I almost forgot, here is the video on how to complete it: https://www.youtube.com/watch?v=OXmvZFuc2tA&t=22s

Controls:

WASD to move, mouse to look.

Left Shift to sprint, Space to jump, Left Ctrl to crouch.

Right mouse to zoom.

F to interact, inspect or pick things up.

E to use the car. Talk to the cashier.

M for the quest log.

Esc to pause or close a menu.

Left click to advance dialogue.

In the car: W/S for throttle and reverse, A/D to steer, mouse to look E to get out.

If you want to open the project in Unity, it is Unity 6.7 alpha (6000.7.0a6). Open Assets/Scenes/. Press Play. The build settings already have the scenes in order.

I used third-party art, environment, music and sound packs. Their licenses are in their folders. The main ones are ARCADE. Racing Car, Gridness Studios Grocery Store Pack Lite, LowlyPoly, Layer Lab 3D Props-AdorableFoods, Mnostva Art Food, Halloween Pumpkin, Modular Lowpoly Streets Free, Real Stars Skybox, vrbn studios city buildings, BxB Studio, AN Interactive Physical Door Pack, Mono Limbo, DearMousey, npc_casual_set_00, Sat Productions and the music and sound packs.

Special thanks to Pinyuan Chen, who tested the game (and gave it a 0 out of 10 because it scared him) and to Paya, who helped me with the idea.

Thanks for reading. If you play it, tell me what broke or what confused you. I would like to make it better.

by Serali Totay (Cereal)

-or Nickname ciklole1
