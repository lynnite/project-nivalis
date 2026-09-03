using Robust.Shared.Prototypes;



namespace Content.Shared._Nivalis.Perks;

/// <summary>
///     Raised on a survivor when their equipped perk ability is triggered and its cooldown is
///     satisfied. Perk-specific logic subscribes to this and checks the perk id to perform its effect.
/// </summary>
public readonly record struct NivalisPerkUsedEvent(ProtoId<NivalisPerkPrototype> Perk);
