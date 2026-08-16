using Robust.Shared.Serialization;

namespace Content.Shared._Nivalis.GameTicking.Components;

[Serializable, NetSerializable]
public enum NivalisSurvivalPhase : byte
{
    Scavenge,

    Storm,
}
