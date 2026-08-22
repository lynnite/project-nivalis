using Robust.Shared.Map;
using Robust.Shared.Serialization;

namespace Content.Shared._Nivalis.Melee.Events;

[Serializable, NetSerializable]
public sealed class NivalisShoveEvent : EntityEventArgs
{
    public readonly NetEntity Target;
    public readonly NetCoordinates Coordinates;

    public NivalisShoveEvent(NetEntity target, NetCoordinates coordinates)
    {
        Target = target;
        Coordinates = coordinates;
    }
}
