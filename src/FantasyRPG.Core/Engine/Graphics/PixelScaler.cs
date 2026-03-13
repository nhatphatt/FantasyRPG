using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace FantasyRPG.Core.Engine.Graphics;

/// <summary>
/// Owns the low-resolution RenderTarget2D and provides helpers for
/// the two-pass pixel-perfect rendering pipeline.
/// </summary>
public sealed class PixelScaler
{
    private readonly RenderTarget2D _target;
    private readonly GraphicsDevice _graphicsDevice;

    public RenderTarget2D Target => _target;
    public int VirtualWidth => _target.Width;
    public int VirtualHeight => _target.Height;

    public PixelScaler(RenderTarget2D target, GraphicsDevice graphicsDevice)
    {
        _target = target;
        _graphicsDevice = graphicsDevice;
    }

    /// <summary>
    /// Activates the low-res render target for world drawing.
    /// Call this before your world-space SpriteBatch.Begin().
    /// </summary>
    public void BeginScene()
    {
        _graphicsDevice.SetRenderTarget(_target);
        _graphicsDevice.Clear(GameSettings.ClearColor);
    }

    /// <summary>
    /// Switches back to the backbuffer for the upscale pass.
    /// Call this after your world-space SpriteBatch.End().
    /// </summary>
    public void EndScene()
    {
        _graphicsDevice.SetRenderTarget(null);
    }

    /// <summary>
    /// Converts a screen-space point (mouse click) to virtual pixel coordinates.
    /// Uses the pre-computed destination rectangle to reverse the scaling.
    /// </summary>
    public Vector2 ScreenToVirtual(in Vector2 screenPosition, in Rectangle destinationRect)
    {
        float x = (screenPosition.X - destinationRect.X) / destinationRect.Width * VirtualWidth;
        float y = (screenPosition.Y - destinationRect.Y) / destinationRect.Height * VirtualHeight;
        return new Vector2(x, y);
    }
}
