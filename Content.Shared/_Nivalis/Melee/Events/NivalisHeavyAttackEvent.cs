using Robust.Shared.Map;
using Robust.Shared.Serialization;

namespace Content.Shared._Nivalis.Melee.Events;

[Serializable, NetSerializable]
public sealed class NivalisHeavyAttackEvent : NivalisAttackEvent
{
    public readonly NetEntity Weapon;

    public List<NetEntity> Entities;

    public NivalisHeavyAttackEvent(NetEntity weapon, List<NetEntity> entities, NetCoordinates coordinates)
        : base(coordinates)
    {
        Weapon = weapon;
        Entities = entities;
    }
}
