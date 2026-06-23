/// <summary>
/// ╔══════════════════════════════════════════════════════════════╗
/// ║           MAZE GAME – UNITY SCENE SETUP GUIDE               ║
/// ║        Read this before opening Unity for the first time     ║
/// ╚══════════════════════════════════════════════════════════════╝
///
/// UNITY VERSION: Unity 6 LTS (6000.x)
/// PROJECT TYPE:  3D (Built-in Render Pipeline)
///
/// ──────────────────────────────────────────────────────────────
/// STEP 1 – PROJECT CREATION
/// ──────────────────────────────────────────────────────────────
/// 1. Open Unity Hub → New Project → "3D (Built-in RP)" template.
/// 2. Name: "MazeGame"
/// 3. Import TextMeshPro (Window → TextMeshPro → Import TMP Essential Resources).
/// 4. Copy ALL .cs files from the Scripts folder into Assets/Scripts/.
///
/// ──────────────────────────────────────────────────────────────
/// STEP 2 – TAGS & LAYERS
/// ──────────────────────────────────────────────────────────────
/// Add these Tags (Edit → Project Settings → Tags and Layers):
///   • Player
///   • Ground
///   • Wall
///   • Obstacle
///   • Checkpoint
///   • Finish
///
/// ──────────────────────────────────────────────────────────────
/// STEP 3 – HIERARCHY STRUCTURE
/// ──────────────────────────────────────────────────────────────
/// Create the following GameObjects in your Scene:
///
/// [_MANAGERS]         (Empty GameObject – holds all manager scripts)
///   ├── GameManager         → Add GameManager.cs
///   ├── ScoreManager        → Add ScoreManager.cs
///   ├── LeaderboardManager  → Add LeaderboardManager.cs
///   ├── CheckpointManager   → Add CheckpointManager.cs
///   ├── AudioManager        → Add AudioManager.cs
///   └── UIManager           → Add UIManager.cs
///
/// [Player]            (Sphere, Tag = "Player")
///   ├── Add Component: Rigidbody (mass=1, drag=2, angular drag=0.05, freeze rotation XYZ)
///   ├── Add Component: SphereCollider
///   ├── Add Component: PlayerController.cs
///   └── Child: [PlayerVisual] – Sphere mesh renderer only (no collider)
///
/// [MazeGenerator]     (Empty, holds MazeGenerator.cs – OR manually built maze below)
///
/// [Maze]              (Parent for all maze geometry if building manually)
///   ├── Floor          – Plane/Cube, Tag="Ground"
///   ├── Walls          – Multiple Cubes, Tag="Wall"
///   ├── StartZone      – Cube trigger, Add StartZone.cs (green material)
///   ├── FinishZone     – Cube trigger, Add FinishZone.cs (gold material)
///   ├── DeathZone      – Large trigger BELOW maze floor, Add DeathZone.cs
///   └── Checkpoints/
///         └── Checkpoint_01  – Cube trigger, Add Checkpoint.cs, id=0
///
/// [Obstacles]
///   ├── RotatingObstacle_01   – Add RotatingObstacle.cs + MeshCollider
///   ├── MovingObstacle_01     – Add MovingObstacle.cs; create PointA, PointB child empties
///   ├── SpikeTrap_01          – Add SpikeTrapObstacle.cs; child [SpikeMesh] with Collider
///   └── AIEnemy               – Capsule/Cube, Add AIObstacle.cs; add [Waypoint1..4] children
///
/// [CameraRig]
///   └── Main Camera           – Add CameraController.cs
///
/// [UI]                (Canvas, Screen Space - Overlay)
///   ├── StartPanel            – Panel with "Start" button → calls UIManager.OnStartButtonPressed()
///   ├── HUD                   – Score text, Timer text, Penalty feedback text, Camera toggle button
///   ├── PausePanel            – Resume/Restart/MainMenu buttons + SensitivitySlider
///   ├── WinPanel              – Final score text, Best score text, Leaderboard container
///   └── LosePanel             – "You Fell!" text + Restart button
///
/// ──────────────────────────────────────────────────────────────
/// STEP 4 – INSPECTOR WIRING
/// ──────────────────────────────────────────────────────────────
/// GameManager:
///   Drag Player, ScoreManager, UIManager, LeaderboardManager, CheckpointManager into slots.
///
/// UIManager:
///   Drag all 5 panel CanvasGroups into Panel slots.
///   Drag HUD text fields into their slots.
///   Wire all buttons to UIManager methods via OnClick().
///
/// CameraController:
///   Drag Player Transform into "Target" field.
///
/// MovingObstacle:
///   Create two empty child GameObjects (PointA, PointB) at desired end positions.
///   Drag them into PointA / PointB fields.
///
/// AIObstacle:
///   Create 3-4 empty GameObjects as waypoints inside or near the maze corridors.
///   Drag them into the Waypoints array.
///
/// ──────────────────────────────────────────────────────────────
/// STEP 5 – PHYSICS MATERIALS
/// ──────────────────────────────────────────────────────────────
/// Create Physics Materials (Assets → Create → Physics Material):
///   • BallMaterial:   friction=0.3, bounciness=0.1  → assign to Player's SphereCollider
///   • WallMaterial:   friction=0.5, bounciness=0.0  → assign to all Wall Colliders
///   • FloorMaterial:  friction=0.6, bounciness=0.0  → assign to Floor Collider
///
/// ──────────────────────────────────────────────────────────────
/// STEP 6 – ANDROID BUILD SETTINGS
/// ──────────────────────────────────────────────────────────────
/// File → Build Settings → Android → Switch Platform
/// Player Settings:
///   • Company Name: [YourName]
///   • Product Name: MazeGame
///   • Minimum API Level: Android 7.0 (API 24)
///   • Target API Level: Automatic
///   • Scripting Backend: IL2CPP
///   • Target Architectures: ARM64 ✓ (ARMv7 optional)
///   • Orientation: Landscape Left (accelerometer works best)
///   • Internet Access: Not Required
///
/// ──────────────────────────────────────────────────────────────
/// STEP 7 – MAZE LAYOUT GUIDE (Manual Build)
/// ──────────────────────────────────────────────────────────────
/// The maze should have 5-8 visible navigation paths.
/// Recommended layout (grid notation, S=Start, F=Finish, C=Checkpoint):
///
///   ┌───┬───┬───┬───┬───┐
///   │   │   │   │   │ F │
///   ├   ┼───┤   ├───┤   │
///   │   │   │   │   │   │
///   ├───┤   ├───┤   ├   │
///   │   │   │   │ C │   │
///   ├   ├───┤   ├───┤   │
///   │   │   │   │   │   │
///   ├───┤   ├───┤   ├   │
///   │ S │   │   │   │   │
///   └───┴───┴───┴───┴───┘
///
/// Each cell = 3 Unity units. Walls are 0.3 thick, 2 units tall.
/// Add wider corridors (6 units) near obstacles to give the player room to react.
///
/// ──────────────────────────────────────────────────────────────
/// STEP 8 – PREFABS TO CREATE
/// ──────────────────────────────────────────────────────────────
/// Wall Prefab:       Cube, scale=(1,1,1), white material, tag="Wall", Layer=Default
/// Floor Prefab:      Cube, scale=(3,0.1,3), grey material, tag="Ground"
/// Player Prefab:     Sphere with Rigidbody + PlayerController
/// Leaderboard Entry: TextMeshPro - Text component only (used by UIManager.PopulateLeaderboard)
/// </summary>
public static class SceneSetupGuide
{
    // This class is a documentation-only placeholder.
    // All instructions are in the XML summary comment above.
    // Reference this file when setting up the Unity scene.
}
