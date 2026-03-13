namespace FantasyRPG.Core.Components;

/// <summary>
/// Immutable definition of a single animation clip (e.g., "Idle", "Run", "Jab").
/// Stored in a pre-allocated array per character. NEVER modified at runtime.
///
/// Defines which region of a sprite sheet to sample:
///   - Row = which row of the sheet (Y offset = Row * FrameHeight)
///   - StartFrame / FrameCount = which columns (X offset = frame * FrameWidth)
///   - FrameDuration = seconds per frame (at 60fps: 0.1 = 6 frames per sprite frame)
///   - Looping = whether to repeat or freeze on last frame
///
/// This struct is 20 bytes — fits comfortably in cache.
/// </summary>
public readonly struct AnimationData
{
    /// <summary>Row index in the sprite sheet (0-based).</summary>
    public readonly int Row;

    /// <summary>First frame column in this row.</summary>
    public readonly int StartFrame;

    /// <summary>Number of frames in this animation.</summary>
    public readonly int FrameCount;

    /// <summary>Seconds per animation frame. 0.1 = 10fps animation, 0.067 = 15fps.</summary>
    public readonly float FrameDuration;

    /// <summary>If true, wraps back to StartFrame. If false, holds on last frame.</summary>
    public readonly bool Looping;

    public AnimationData(int row, int startFrame, int frameCount, float frameDuration, bool looping)
    {
        Row = row;
        StartFrame = startFrame;
        FrameCount = frameCount;
        FrameDuration = frameDuration;
        Looping = looping;
    }
}
