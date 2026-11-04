# Music

The game uses a data-driven music layer. Songs live in one asset, and a
persistent `MusicManager` plays the right cue for each scene and story beat.
No story scene needs a music component, so `City.unity`, `Store.unity` and the
speech folders stay untouched.

## Quick start

1. Put your music files in `Assets/Sounds/Music/` (wav, mp3 or ogg).
2. Open `Assets/Resources/Paralysis/MusicDatabase.asset`.
   - Or use **Paralysis > Music > Select Music Database**.
3. Expand a cue and drag a clip into **Clip**.
4. Press Play. The manager creates itself automatically and plays the cue
   bound to the current scene.

The database ships with every cue already named, but all clip slots are empty
because the project did not contain any music files yet. A cue with no clip
keeps the previous music playing instead of cutting to silence, so you can add
tracks one at a time.

## Built-in cues

| Cue id | When it plays |
| --- | --- |
| `menu` | Main menu and chapter selection |
| `start` | Opening of the apartment (`Home`) |
| `home_lights_off` | Optional, after the last apartment light is switched off |
| `city` | City street scene |
| `car_arc` | Driving scene (`CarDriving`) |
| `car_arrival` | Optional, when the car reaches the store marker (timed, one-shot) |
| `store` | Store shopping scene (timed grocery theme, one-shot) |
| `store_ready_to_pay` | Optional, after every grocery item is collected |
| `store_checkout` | Optional, when the successful cashier conversation starts |
| `scary_checkout` | When the cashier transforms |
| `end` | End-of-demo screen (timed, one-shot) |

The act themes loop for as long as the act lasts. The optional cues are safe to
leave empty: the current track simply keeps playing.

## Looping and timed songs

Every cue has a **Loop** checkbox (the `loop` field in the asset):

- **On** - the track repeats until another cue replaces it. Use this for
  background and act themes such as `menu`, `start`, `city` and `car_arc`.
- **Off** - one-shot. The track plays exactly once, then stops, and the manager
  clears the cue so the next request starts from a clean state. Use this for
  timed songs: the grocery store theme (`store`), the car arrival sting
  (`car_arrival`) and the ending theme (`end`) ship this way.

The cue foldout shows `[loop]` or `[one-shot]` next to its name, so you can see
the mode without expanding it. `MusicManager.CurrentCueLoops` and
`CurrentCueIsOneShot` expose the same state to code if an act ever needs to
branch on it.

If a timed cue is accidentally switched back to looping, run
`Paralysis > Music > Apply Timed Cue Defaults` to restore the shipped one-shot
mode for `store`, `car_arrival` and `end`.

## Scene bindings

The **Scenes** list maps a scene name to a cue id. The shipped bindings are:

| Scene | Cue |
| --- | --- |
| `MainMenu` | `menu` |
| `Home` | `start` |
| `City` | `city` |
| `CarDriving` | `car_arc` |
| `Store` | `store` |
| `End` | `end` |

Scene names must match Build Settings exactly. Use **Add Missing Scene
Bindings** in the Inspector to restore the defaults.

If a scene has no binding, `keepMusicWhenUnbound` leaves the current track
alone. You can instead set a `fallbackCueId`, or turn the option off to stop
the music when an unbound scene loads.

## Cue settings

- **Clip** - the song or sting.
- **Volume** - per-cue level, multiplied by the database master volume.
- **Loop** - keep playing, or play once (useful for arrival stings).
- **Fade In / Fade Out** - crossfade times in seconds.
- **Pitch** - playback speed/pitch.
- **Stinger** - optional one-shot sound layered on top when the cue starts.
- **Restart When Loaded** - restart the track on a scene reload instead of
  continuing it.

Set the overall level with **Master Volume** on the database. At runtime:

```csharp
MusicManager.SetVolume(0.5f);   // 0..1 player-facing scale
MusicManager.SetMuted(true);
MusicManager.StopMusic(2f);     // fade out over two seconds
```

## Adding more acts or songs

1. Click **Add Cue** in the database Inspector.
2. Give it a unique id, for example `rooftop`.
3. Assign a clip.
4. Either bind a scene to it in **Scenes**, or call it from a script at the
   exact story beat:

```csharp
MusicManager.PlayCue("rooftop");
MusicManager.PlayCue("rooftop", 0.5f); // custom crossfade
MusicManager.RestartCue("rooftop");    // start from the beginning
```

`PlayCue` returns `false` when the cue is missing or has no clip. It never
stops the current music in that case.

## Previewing

Enter Play Mode, select the database, expand a cue and press **Preview**. The
**Stop Music** button next to **Validate** stops the current track. In Edit
Mode the preview button is disabled.

## Recommended import settings

For full-length songs, select the clip in the Project window and set:

- **Load Type**: Streaming (keeps memory low)
- **Compression Format**: Vorbis
- **Quality**: around 70-100

Short stings can use Decompress On Load.

## Troubleshooting

- **No music in a scene** - check that the scene has a binding and that the
  cue has a clip. The Console prints one note per cue that is still empty.
- **Music cuts out** - make sure the cue's **Loop** is on and the clip's
  **Load Type** is not set to a very small compressed buffer.
- **Wrong song** - check the scene name spelling in **Scenes** and the cue id.
- **No sound at all** - check **Master Volume** and the game audio listener.
  `MusicManager` creates its own AudioSources and does not touch the dialogue
  voice sources.

## Speech is separate

The music manager only uses its own `AudioSource` objects. It never reads,
reassigns or modifies anything in `Assets/Sounds/Speeches/` or
`Assets/Sounds/Human speeches/`, so the recorded dialogue keeps working
exactly as before.
