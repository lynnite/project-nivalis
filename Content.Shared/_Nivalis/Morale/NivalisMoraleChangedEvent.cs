namespace Content.Shared._Nivalis.Morale;

[ByRefEvent]
public readonly record struct NivalisMoraleChangedEvent(
    EntityUid Survivor,
    NivalisMoraleLevel OldLevel,
    NivalisMoraleLevel NewLevel);
