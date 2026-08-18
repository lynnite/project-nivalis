using Content.Shared.Damage;
using Robust.Shared.Map;
using Robust.Shared.Serialization;

namespace Content.Shared._Nivalis.Melee.Events;

[Serializable, NetSerializable]
public abstract class NivalisAttackEvent : EntityEventArgs
{
    public readonly NetCoordinates Coordinates;

    protected NivalisAttackEvent(NetCoordinates coordinates)
    {
        Coordinates = coordinates;
    }
}

public sealed class NivalisAttackedEvent : EntityEventArgs
{
    public EntityUid Used { get; }

    public EntityUid User { get; }

    public EntityCoordinates ClickLocation { get; }

    public DamageSpecifier BonusDamage = new();

    public NivalisAttackedEvent(EntityUid used, EntityUid user, EntityCoordinates clickLocation)
    {
        Used = used;
        User = user;
        ClickLocation = clickLocation;
    }
}
