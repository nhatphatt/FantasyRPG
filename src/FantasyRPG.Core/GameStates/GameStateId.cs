namespace FantasyRPG.Core.GameStates;

/// <summary>
/// Enum-based state identifiers. Used as keys for the GameStateManager.
/// Avoids string-based lookups and dictionary allocations.
/// </summary>
public enum GameStateId : byte
{
    MainMenu = 0,
    Gameplay = 1,
    Pause = 2,
    GameOver = 3,
    Loading = 4,
    CharacterSelect = 5
}
