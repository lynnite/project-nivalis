using Robust.Shared.Serialization;

namespace Content.Shared._Nivalis.Perks;

[Serializable, NetSerializable]
public sealed class NivalisLazarusBuffMessage : EntityEventArgs
{
    public float Duration;

    public NivalisLazarusBuffMessage(float duration)
    {
        Duration = duration;
    }
}
