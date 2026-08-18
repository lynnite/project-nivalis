using Robust.Shared.Serialization;

namespace Content.Shared._Nivalis.Melee.Events;

[Serializable, NetSerializable]
public sealed class NivalisStopAttackEvent : EntityEventArgs
{
    public readonly NetEntity Weapon;

    public NivalisStopAttackEvent(NetEntity weapon)
    {
        Weapon = weapon;
    }
}
