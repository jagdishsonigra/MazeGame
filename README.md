# 🎮 Maze Game

> A physics-based mobile maze game built in **Unity 6 LTS** where the player navigates a ball from the Start Zone to the End Zone while avoiding obstacles and achieving the highest possible score.

---

## 📋 Table of Contents

- [Project Overview](#project-overview)
- [Features](#features)
- [Controls](#controls)
- [Game Systems](#game-systems)
  - [Player Movement](#player-movement)
  - [Maze Layout](#maze-layout)
  - [Obstacle System](#obstacle-system)
  - [Checkpoint System](#checkpoint-system)
  - [Camera System](#camera-system)
  - [Scoring System](#scoring-system)
  - [UI States](#ui-states)
  - [Leaderboard](#leaderboard)
- [Technical Decisions](#technical-decisions)
- [Project Structure](#project-structure)
- [How to Run](#how-to-run)
- [Challenges Faced](#challenges-faced)
- [Future Improvements](#future-improvements)
- [AI Usage Declaration](#ai-usage-declaration)

---

## 🗂 Project Overview

**Maze Game** is a single-scene, physics-driven mobile puzzle game. The player controls a sphere rolling through a hand-crafted maze with multiple navigable paths, three distinct obstacle types, one checkpoint, two camera modes, a real-time scoring system, and a persistent local leaderboard.

| Field | Detail |
|---|---|
| Engine | Unity 6 LTS |
| Language | C# |
| Platform Target | Android (Mobile) + Unity Editor (PC) |
| Input | Accelerometer (Mobile) / WASD & Arrow Keys (PC) |
| Scene Count | 1 (Single-scene with panel-based UI states) |
| Render Pipeline | Universal Render Pipeline (URP) |

---

## ✅ Features

### Core Requirements
- [x] Physics-based sphere movement using `Rigidbody`
- [x] Accelerometer input on mobile, keyboard input in Editor
- [x] Adjustable input sensitivity via in-game slider
- [x] Maze with 5–8 navigable paths, clearly marked Start and End zones
- [x] One life — no respawn (reaches Lose screen on death/obstacle game-over)
- [x] 3 unique obstacle types with a penalty system
- [x] Start, Pause, Win, and Lose UI screens
- [x] Scoring system based on time, penalties, and remaining score

### Additional Requirements
- [x] Checkpoint system (saves player state mid-maze)
- [x] Two camera modes: **Follow Camera** and **Top-Down Camera** with live switching
- [x] Simple AI Obstacle with patrol and follow behaviour
- [x] Local leaderboard storing Top 5 scores, sorted and persisted via `PlayerPrefs`

---

## 🕹 Controls

### Unity Editor / PC

| Action | Key |
|---|---|
| Move Forward | `W` / `↑` Arrow |
| Move Backward | `S` / `↓` Arrow |
| Move Left | `A` / `←` Arrow |
| Move Right | `D` / `→` Arrow |
| Pause Game | `Escape` |
| Switch Camera Mode | Camera Mode Button (UI) |

### Android / Mobile

| Action | Input |
|---|---|
| Move | Tilt device (Accelerometer) |
| Pause | Tap Pause button (HUD) |
| Switch Camera | Tap Camera Mode button (HUD) |

> **Sensitivity** can be adjusted from the Start Screen using the sensitivity slider. This scales accelerometer and keyboard input force to suit player preference.

---

## ⚙️ Game Systems

### Player Movement

The player is a Unity `Sphere` GameObject with a `Rigidbody` component. Movement is physics-based — force is applied to the Rigidbody each `FixedUpdate` frame rather than directly setting transform position.

**Platform detection at runtime:**
- On mobile builds, `Input.acceleration` from the device accelerometer drives the movement vector.
- In the Unity Editor / PC builds, keyboard input via Unity's **New Input System** (`WASD` / Arrow Keys) is used.
- A **sensitivity multiplier** (exposed via a UI slider) scales the input force, letting players tune responsiveness without changing the physics setup.

**Physics materials** (`Ball Material.physicMaterial` and `FloorMaterial.physicMaterial`) are configured to provide appropriate friction and bounciness so the ball rolls naturally without sliding uncontrollably.

---

### Maze Layout

The maze is a static geometry object (`MazeGeometry Final`) built from scaled cube primitives with a Wall physics material applied for consistent collision response. It features:

- **6+ distinct navigation paths** from Start Zone to End Zone
- A clearly coloured and labelled **Start Zone** (green trigger zone)
- A clearly coloured and labelled **End Zone / Finish** (trigger zone, initiates Win state)
- A **DeathZone** (invisible trigger below the maze) — falling off the maze triggers the Lose screen immediately (one life only)

---

### Obstacle System

Three obstacle types are placed throughout the maze, each applying a different score penalty on collision:

| # | Name | Type | Behaviour | Penalty |
|---|---|---|---|---|
| 1 | Rotating Obstacle | Static Hazard | Continuously rotates on its Y-axis in place, blocking narrow corridors | −50 points |
| 2 | Spike Obstacle | Static Hazard | Fixed spike cluster — contact triggers a penalty but does not kill the player | −75 points |
| 3 | AI Obstacle | Dynamic Agent | Patrols between two waypoints (Point A → Point B). Switches to **Follow** mode when the player enters its detection radius | −100 points |

**Penalty System Design:**
- Each collision triggers a one-time penalty (not continuous drain) using a cooldown flag to prevent repeated deductions from a single contact.
- A **Penalty Feedback Text** element briefly flashes on the HUD to inform the player of the penalty amount.
- Score cannot go below 0.
- After a set number of penalty hits (configurable), the Lose screen is triggered — enforcing the "one life" spirit while giving the player a chance to recover.

---

### Checkpoint System

A single **Checkpoint** (`Checkpoint_01`, tagged `Checkpoint`) is placed midway through the maze.

**How it works:**
- When the player's ball enters the Checkpoint trigger zone, the player's current world position and score are stored in a `CheckpointData` struct.
- If the player subsequently falls into the DeathZone, instead of an immediate Lose screen, they are respawned at the checkpoint position with the checkpoint score restored.
- Each checkpoint can only be activated once per run (deactivates after first trigger to avoid repeated saves).
- A **"Restart From Checkpoint"** button on the Lose/Pause screen allows returning to the last saved checkpoint.

**Data structure used:** A simple serialisable `CheckpointData` struct holds:
```csharp
struct CheckpointData {
    Vector3 position;
    int score;
    float elapsedTime;
    bool isActive;
}
```
This cleanly separates state from behaviour and makes the checkpoint system easy to extend with multiple checkpoints.

---

### Camera System

Two camera modes are available and can be toggled in-game via the **Camera Mode Button** on the HUD:

| Mode | Description |
|---|---|
| **Follow Camera** | Positioned behind and above the ball, smoothly follows using `Vector3.Lerp`. Gives a 3D perspective and shows obstacles ahead. |
| **Top-Down Camera** | Positioned directly above the maze, looking straight down. Shows the full maze layout — useful for planning a route. |

Switching is instant and handled by repositioning and reorienting the single `Main Camera` GameObject. No second camera object is needed, keeping the scene clean and memory efficient.

---

### Scoring System

Score starts at a base value (e.g., **1000 points**) and is modified in real-time by:

| Factor | Effect |
|---|---|
| Elapsed time | Deducted continuously (e.g., −1 point per second) |
| Obstacle penalty | Deducted on collision (−50 to −100, type-dependent) |
| Reaching End Zone | Remaining score is your **Final Score** |

**Displayed values:**
- **Score Text** (HUD) — updates live every frame
- **Timer Text** (HUD) — shows elapsed time MM:SS
- **Final Score** — shown on the Win screen
- **Best Score** — pulled from the leaderboard and shown on Win screen
- **Penalties** — cumulative penalty total shown on Win screen breakdown

---

### UI States

The game uses a single Canvas with panel-based state management. Only one panel is active at a time, controlled by the `UIManager`:

| Panel | Trigger | Key Elements |
|---|---|---|
| **Start Screen** (`StartPanel`) | On game launch | Play button, Sensitivity Slider, Instructions |
| **HUD** (`HUD`) | During gameplay | Score Text, Timer Text, Pause Button, Camera Mode Button |
| **Pause Screen** (`PausePanel`) | Pause button / Escape | Resume, Restart, Main Menu buttons |
| **Win Screen** (`WinPanel`) | Reaching the End Zone | Final Score, Completion Time, Penalties, Best Score, Restart, Main Menu |
| **Lose Screen** (`LosePanel`) | DeathZone + all hits used | Lose Text, Restart, Restart From Checkpoint, Main Menu |

The **Instructions** panel (multi-page, `Page1`–`Page4` with Next/Previous navigation) is accessible from the Start Screen and explains game mechanics to new players.

---

### Leaderboard

The local leaderboard stores the **Top 5 scores** and persists between sessions using Unity's `PlayerPrefs` with JSON serialisation.

**Implementation details:**
- Scores are stored in a `List<LeaderboardEntry>` where each entry holds a rank, score, and completion time.
- On a run completion, the new score is inserted into the sorted list and the list is trimmed to 5 entries.
- Sorting is descending by score (higher is better).
- The leaderboard is displayed in a table (`Row1`–`Row5`) on a dedicated **Leaderboard** panel, accessible from the Win screen.
- Entries persist across app sessions and device restarts.

**Data structure:**
```csharp
[Serializable]
class LeaderboardEntry {
    public int rank;
    public int score;
    public float completionTime;
}
```

---

## 🧠 Technical Decisions

### Why a Single Scene?
Using a single scene with panel-based UI avoids `SceneManager.LoadScene` loading times between states, making transitions feel instant — which is important for mobile UX. All state is managed in memory, reducing complexity.

### Why Physics-Based Movement instead of Transform?
Using `Rigidbody.AddForce` gives the ball realistic momentum, friction, and collision response with no additional code. It naturally handles edge cases like rolling down ramps or bouncing off walls.

### Why PlayerPrefs + JSON for the Leaderboard?
`PlayerPrefs` is the simplest persistent storage available in Unity across all platforms including Android. Serialising a `List<LeaderboardEntry>` to JSON via `JsonUtility` keeps it human-readable, easy to debug, and extensible (adding a player name field later is trivial).

### Why a Single Main Camera with Mode Switching instead of Two Cameras?
Maintaining two active cameras introduces overdraw and confusion about which camera is rendering. A single camera that repositions is simpler, cheaper, and easier to debug.

### AI Obstacle: State Machine Approach
The AI obstacle uses a two-state finite state machine (Patrol ↔ Follow). This is the correct pattern for simple game AI — it is readable, debuggable, and extends naturally (adding a "Return to Patrol" timer, a Chase state, etc.) without rewriting logic.

### New Input System (Unity Input System Package)
Using Unity's new Input System (`InputSystem_Actions.inputactions`) future-proofs the controls — the same action map works for keyboard, gamepad, and touch with zero code changes. Accelerometer is layered on top at runtime by checking `SystemInfo.deviceType`.

---

## 📁 Project Structure

```
Assets/
├── Scenes/
│   └── SampleScene.unity          # Single game scene (all states)
├── Scripts/
│   ├── PlayerController.cs        # Physics movement, accelerometer + keyboard input, sensitivity
│   ├── CameraController.cs        # Follow and Top-Down camera modes, smooth follow
│   ├── ObstacleBase.cs            # Base class for all obstacles
│   ├── RotatingObstacle.cs        # Rotates continuously
│   ├── SpikeObstacle.cs           # Static spike hazard
│   ├── AIObstacle.cs              # Patrol + Follow FSM (state-based AI)
│   ├── CheckpointManager.cs       # CheckpointData struct, trigger detection, state save/restore
│   ├── ScoreManager.cs            # Live score, time tracking, penalty application
│   ├── UIManager.cs               # Panel switching, HUD updates
│   ├── LeaderboardManager.cs      # Top 5 sort, PlayerPrefs JSON persistence
│   └── GameManager.cs             # Central game state (Playing, Paused, Win, Lose)
├── Materials/
│   ├── Ball Material.physicMaterial
│   ├── FloorMaterial.physicMaterial
│   ├── WallMaterial.physicMaterial
│   ├── Wall.mat
│   ├── Floor.mat
│   └── Red.mat
├── Prefabs/
│   └── Wall.prefab                # Reusable wall segment prefab
├── Settings/
│   ├── Mobile_RPAsset.asset       # URP settings for Android
│   ├── PC_RPAsset.asset           # URP settings for PC/Editor
│   ├── Mobile_Renderer.asset
│   └── PC_Renderer.asset
├── TextMesh Pro/                  # TMP package assets for UI text
└── InputSystem_Actions.inputactions  # New Input System action map
```

---

## 🚀 How to Run

### In Unity Editor
1. Open the project in **Unity 6 LTS**.
2. Open `Assets/Scenes/SampleScene.unity`.
3. Press **Play**.
4. Use `WASD` or Arrow Keys to move the ball.

### Android Build
1. In Unity, go to **File → Build Settings**.
2. Select **Android** and click **Switch Platform**.
3. Connect your Android device with USB Debugging enabled.
4. Click **Build and Run** (or install the provided `MazeGame.apk`).
5. Tilt your device to control the ball.

> **Minimum Android API Level:** 22 (Android 5.1+)

---

## 🚧 Challenges Faced

**1. Accelerometer Calibration**
The raw accelerometer vector needed to be mapped to the correct world axes depending on device orientation (portrait vs landscape). Solved by reading `Input.acceleration` and remapping X/Y to the world XZ plane, with the sensitivity multiplier allowing fine-tuning per-device.

**2. Obstacle Penalty Cooldown**
Without a cooldown, `OnCollisionStay` fired every physics frame, draining hundreds of points per second on a single touch. Solved with a `bool _penaltyCooldown` flag and a `Coroutine` that resets it after a short delay, ensuring one penalty deduction per contact event.

**3. Checkpoint State Restoration**
Restoring the player's exact physics state (not just position) was tricky because residual `Rigidbody` velocity caused the ball to shoot off after respawn. Solved by zeroing `rigidbody.velocity` and `rigidbody.angularVelocity` before setting the checkpoint position.

**4. AI Obstacle Following in a Maze**
A simple "move toward player" approach caused the AI to clip through walls. Solved by using Unity's **NavMesh** surface baked on the maze floor, with the AI agent using `NavMeshAgent` for pathfinding — so it follows corridors, not straight lines through geometry.

**5. Single-Scene UI State Management**
Managing which panel is active without a dedicated scene per state required careful ordering — disabling the previous panel before enabling the next to avoid input conflicts between overlapping UI elements. Addressed with a centralised `UIManager.ShowPanel(Panel)` method.

---

## 🔮 Future Improvements

Given additional development time, I would implement:

- **Multiple Levels / Procedural Maze Generation** — The inactive `MazeGenerator` GameObject in the scene is a starting point for Wilson's algorithm or recursive backtracker maze generation, which would provide unlimited replayability.
- **Sound Design & Music** — Background music, a rolling ball sound (pitch-shifted by velocity), and distinct sounds per obstacle type and UI interaction.
- **Animated Obstacles** — Spike obstacles that retract and extend on a timer, adding rhythm-based challenge.
- **Player Name Entry for Leaderboard** — A text input field on the Win screen to personalise leaderboard entries.
- **Haptic Feedback on Mobile** — Short vibrations on obstacle hit and checkpoint activation using Unity's `Handheld.Vibrate()`.
- **Visual Polish** — Particle effects on the ball (trail renderer), checkpoint activation glow, and end zone portal animation.
- **Multiple Checkpoints** — Extending the `CheckpointData` list to support a queue of checkpoints the player passes through.

---

## 🤖 AI Usage Declaration

Reliance Games encourages responsible use of AI-assisted development tools. This section documents how AI tools were used during this project.

---

### AI Tools Used

| Tool | Role |
|---|---|
| **GitHub Copilot** (in VS Code) | In-editor code completion and line-level suggestions |
| **Claude (Anthropic)** | Architecture discussion, boilerplate generation, debugging assistance |

---

### Development Usage

#### GitHub Copilot
Copilot was used **inline in VS Code** throughout development. Its primary contributions were:
- **Auto-completing repetitive boilerplate** — property declarations, `null` checks, Unity lifecycle method stubs (`Awake`, `Start`, `Update`, `FixedUpdate`, `OnTriggerEnter`).
- **Completing known patterns** — Once I wrote the first few lines of a pattern I already understood (e.g., a scoring deduction on collision), Copilot completed the rest of the block correctly, saving typing time.
- **Minor utility helpers** — String formatting for the timer display (`MM:SS`), `PlayerPrefs` key name constants.

Copilot did **not** design any system. Every suggestion was reviewed before acceptance, and incorrect suggestions (e.g., using `Update` instead of `FixedUpdate` for physics) were rejected and corrected manually.

Estimated contribution: **line-completion and repetitive code (~30–40% of total lines typed, 0% of architectural decisions).**

#### Claude (Anthropic)
Claude was used in **three focused conversations** during development:

1. **Architecture discussion (pre-coding):** Asked Claude to list pros/cons of single-scene vs multi-scene state management for a mobile Unity game. Used the output to confirm my decision to use panel-based UI in a single scene.

2. **AI Obstacle FSM structure:** Described the patrol + follow requirement and asked Claude to generate a skeleton FSM class. The generated code used a C# `enum` for states and a `switch` statement — a valid pattern I then adapted to use `NavMeshAgent` instead of direct `Transform` movement (the generated version used `Transform.MoveTowards` which ignored walls).

3. **Leaderboard serialisation:** Asked Claude to show how to serialise a `List<T>` to `PlayerPrefs` using `JsonUtility`. Used the pattern as written after verifying it worked in the Unity Editor.

Estimated contribution: **~25–30% of final code influenced by Claude output, primarily in boilerplate and patterns. Core game logic (movement physics, penalty system design, checkpoint state save/restore, camera switching) was designed and implemented manually.**

---

### Representative Prompt Examples

**Prompt 1 — Architecture Decision:**
> *"I'm building a mobile Unity maze game with Start, Pause, Win, and Lose screens. Should I use separate scenes for each state or a single scene with UI panels? What are the trade-offs for mobile performance?"*

Used to validate my design decision. Claude outlined the benefits of panel-based single-scene (faster transitions, no load times, simpler state sharing) vs. multi-scene (cleaner separation but loading overhead). Chose single-scene based on this.

**Prompt 2 — AI Obstacle Skeleton:**
> *"Write a Unity C# script for a simple AI obstacle that patrols between two Transform waypoints (PointA, PointB) and switches to following the player when the player enters a detection radius. Use an enum-based state machine."*

Generated a working skeleton using `Transform.MoveTowards`. I then replaced the movement with `NavMeshAgent.SetDestination` so the AI correctly navigates maze corridors. The enum FSM structure was kept as-is.

**Prompt 3 — PlayerPrefs JSON Leaderboard:**
> *"Show me how to save and load a List of custom serialisable objects to Unity PlayerPrefs using JsonUtility, with a wrapper class to handle the list serialisation limitation."*

Generated the `JsonHelper` wrapper pattern (Unity's `JsonUtility` does not natively serialise root-level arrays). Used the pattern directly after testing it returns correct results.

---

### Validation Process

**How AI-generated code was verified:**
- Every generated code block was read line-by-line before being placed in the project.
- All scripts were compiled in Unity immediately after pasting to catch errors.
- Runtime behaviour was tested in Play mode in the Unity Editor before being considered accepted.

**Incorrect outputs received and fixed:**
- **Copilot:** Suggested `transform.position +=` for player movement instead of `rb.AddForce()`. Rejected — overriding position bypasses physics and causes tunnelling through walls.
- **Copilot:** Suggested `Update()` for the rotation obstacle's `transform.Rotate` call. Changed to `FixedUpdate()` for frame-rate-independent physics consistency.
- **Claude (AI Obstacle):** Generated obstacle used `Transform.MoveTowards` — walks through walls. Fixed by integrating `NavMeshAgent` and baking a NavMesh on the maze floor.
- **Claude (Leaderboard):** Initial `JsonUtility.ToJson(list)` call on a raw `List<>` returned `{}` (empty). Claude's own follow-up explained the wrapper class requirement, which was then used correctly.

**General approach:** AI tools were used to accelerate writing code I already understood how to write — not to solve problems I did not understand. Where generated code had bugs, I diagnosed and fixed them independently, which is itself evidence of understanding the implementation.

---

*README authored by the developer. AI tools used as described above.*