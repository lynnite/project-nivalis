using Robust.Shared.Serialization;

namespace Content.Shared._Nivalis.Morale;

[Serializable, NetSerializable]
public enum NivalisMoraleLevel : byte
{
    High,

    Normal,

    Low,

    Critical,
}
