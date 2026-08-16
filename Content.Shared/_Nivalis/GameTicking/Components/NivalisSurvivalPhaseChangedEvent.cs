namespace Content.Shared._Nivalis.GameTicking.Components;

[ByRefEvent]
public readonly record struct NivalisSurvivalPhaseChangedEvent(
    EntityUid Rule,
    NivalisSurvivalPhase OldPhase,
    NivalisSurvivalPhase NewPhase);
