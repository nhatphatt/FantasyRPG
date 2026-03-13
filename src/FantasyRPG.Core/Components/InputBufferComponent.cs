namespace FantasyRPG.Core.Components;

/// <summary>
/// Fixed-size circular input buffer. Stores the last N frames of input
/// so that actions pressed during recovery/hitstun execute on the first
/// actionable frame. Essential for responsive fighting game feel.
///
/// At 60fps with a 6-frame buffer = 100ms of input leniency.
///
/// Implementation: Ring buffer over a fixed-size array. Zero allocation.
/// Each slot stores which action was pressed and how many frames ago.
/// </summary>
public struct InputBufferComponent
{
    /// <summary>How many frames to retain buffered inputs.</summary>
    public const int BufferSize = 8;

    /// <summary>
    /// Fixed-size ring buffer. Each slot holds the action pressed on that frame.
    /// Index 0 = oldest buffered frame. Written circularly.
    /// </summary>
    public InputSnapshot Buffer0;
    public InputSnapshot Buffer1;
    public InputSnapshot Buffer2;
    public InputSnapshot Buffer3;
    public InputSnapshot Buffer4;
    public InputSnapshot Buffer5;
    public InputSnapshot Buffer6;
    public InputSnapshot Buffer7;

    /// <summary>Current write head position in the ring buffer.</summary>
    public int WriteHead;

    /// <summary>
    /// Records the current frame's input into the buffer.
    /// Called once per frame by the InputBufferSystem, BEFORE combat resolution.
    /// </summary>
    public void RecordInput(InputAction action)
    {
        SetSlot(WriteHead, new InputSnapshot(action, 0));
        WriteHead = (WriteHead + 1) % BufferSize;
    }

    /// <summary>
    /// Ages all buffered inputs by 1 frame. Called once per frame.
    /// Inputs that exceed BufferSize frames old are effectively expired
    /// (overwritten by the ring buffer naturally).
    /// </summary>
    public void AgeBuffer()
    {
        for (int i = 0; i < BufferSize; i++)
        {
            InputSnapshot snap = GetSlot(i);
            if (snap.Action != InputAction.None)
            {
                SetSlot(i, new InputSnapshot(snap.Action, snap.FramesAgo + 1));
            }
        }
    }

    /// <summary>
    /// Consumes the most recent buffered input matching the requested action.
    /// Returns true if found (and clears it). This is the "input buffer" magic:
    /// if the player pressed Attack 3 frames ago during hitstun, and they just
    /// became actionable, this returns true → the attack comes out instantly.
    /// </summary>
    public bool ConsumeAction(InputAction action)
    {
        int bestIndex = -1;
        int bestAge = int.MaxValue;

        for (int i = 0; i < BufferSize; i++)
        {
            InputSnapshot snap = GetSlot(i);
            if (snap.Action == action && snap.FramesAgo < BufferSize && snap.FramesAgo < bestAge)
            {
                bestAge = snap.FramesAgo;
                bestIndex = i;
            }
        }

        if (bestIndex >= 0)
        {
            SetSlot(bestIndex, default); // Clear consumed input
            return true;
        }

        return false;
    }

    /// <summary>
    /// Checks if an action exists in the buffer without consuming it.
    /// </summary>
    public readonly bool HasAction(InputAction action)
    {
        for (int i = 0; i < BufferSize; i++)
        {
            InputSnapshot snap = GetSlotReadOnly(i);
            if (snap.Action == action && snap.FramesAgo < BufferSize)
                return true;
        }
        return false;
    }

    // ── Manual array access (avoids heap-allocated array in struct) ───

    private readonly InputSnapshot GetSlotReadOnly(int index) => index switch
    {
        0 => Buffer0, 1 => Buffer1, 2 => Buffer2, 3 => Buffer3,
        4 => Buffer4, 5 => Buffer5, 6 => Buffer6, 7 => Buffer7,
        _ => default
    };

    private InputSnapshot GetSlot(int index) => index switch
    {
        0 => Buffer0, 1 => Buffer1, 2 => Buffer2, 3 => Buffer3,
        4 => Buffer4, 5 => Buffer5, 6 => Buffer6, 7 => Buffer7,
        _ => default
    };

    private void SetSlot(int index, InputSnapshot value)
    {
        switch (index)
        {
            case 0: Buffer0 = value; break;
            case 1: Buffer1 = value; break;
            case 2: Buffer2 = value; break;
            case 3: Buffer3 = value; break;
            case 4: Buffer4 = value; break;
            case 5: Buffer5 = value; break;
            case 6: Buffer6 = value; break;
            case 7: Buffer7 = value; break;
        }
    }
}

/// <summary>
/// A single buffered input snapshot. Stores what action was pressed
/// and how many frames ago it was pressed.
/// </summary>
public struct InputSnapshot
{
    public InputAction Action;
    public int FramesAgo;

    public InputSnapshot(InputAction action, int framesAgo)
    {
        Action = action;
        FramesAgo = framesAgo;
    }
}

/// <summary>
/// Discrete input actions for the fighting game.
/// Mapped from raw keyboard/gamepad input by the InputSystem.
/// </summary>
public enum InputAction : byte
{
    None = 0,
    Attack = 1,
    Special = 2,
    Block = 3,       // Also initiates Parry if timed correctly
    Jump = 4,
    Dash = 5,
    Ultimate = 6
}
