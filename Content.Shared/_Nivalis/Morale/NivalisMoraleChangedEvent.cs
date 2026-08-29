namespace Content.Shared._Nivalis.Morale;

[ByRefEvent]
public readonly record struct NivalisMoraleChangedEvent(
    EntityUid Survivor,
    int OldMorale,
    int NewMorale);
