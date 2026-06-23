/// <summary>
/// Represents all possible states the game can be in.
/// Used by GameManager to coordinate between systems.
/// </summary>
public enum GameState
{
    MainMenu,
    Playing,
    Paused,
    Win,
    Lose
}
