using Robust.Shared.Serialization;

namespace Content.Shared._Nivalis.Weapons;

[Serializable, NetSerializable]
public sealed class NivalisReloadEvent : EntityEventArgs
{
    public readonly bool Active;

    public NivalisReloadEvent(bool active)
    {
        Active = active;
    }
}

