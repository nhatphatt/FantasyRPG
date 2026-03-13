namespace FantasyRPG.Core.Components;

/// <summary>
/// Integer-indexed animation identifiers. Used as array indices into
/// the character's AnimationData[] lookup table.
/// Avoids string lookups and dictionary allocations.
/// </summary>
public enum AnimationId : byte
{
    Idle = 0,
    Run = 1,
    Jump = 2,        // Rising / airborne
    Fall = 3,        // Falling (optional, can reuse Jump)
    AttackJab = 4,
    AttackHeavy = 5,
    Block = 6,       // Blocking pose
    Parry = 7,       // Parry flash
    Dash = 8,
    Hitstun = 9,
    Count = 10       // Sentinel — always last. Defines array size.
}
