namespace FantasyRPG.Core.Components;

/// <summary>
/// Health and damage tracking for a fighter entity.
/// </summary>
public struct HealthComponent
{
    public float Current;
    public float Max;
    public int StockCount;

    public readonly bool IsDead => Current <= 0f;
    public readonly float Percentage => Max > 0f ? Current / Max : 0f;
}
