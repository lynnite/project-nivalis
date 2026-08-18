using Robust.Shared.Serialization;

namespace Content.Shared._Nivalis.Melee.Events;

[Serializable, NetSerializable]
public sealed class NivalisMeleeParryEvent : EntityEventArgs
{
    public readonly bool Active;

    public NivalisMeleeParryEvent(bool active)
    {
        Active = active;
    }
}
