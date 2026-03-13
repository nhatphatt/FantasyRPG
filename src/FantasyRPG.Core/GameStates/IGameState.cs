using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace FantasyRPG.Core.GameStates;

/// <summary>
/// Contract for a game state (MainMenu, Gameplay, Pause, etc.).
/// All implementations are pre-allocated during LoadContent.
/// </summary>
public interface IGameState
{
    void Enter();
    void Update(GameTime gameTime);
    void Draw(GameTime gameTime, SpriteBatch spriteBatch);
    void Exit();
}
