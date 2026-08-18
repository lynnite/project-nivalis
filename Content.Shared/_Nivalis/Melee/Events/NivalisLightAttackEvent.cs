using Robust.Shared.Map;
using Robust.Shared.Serialization;

namespace Content.Shared._Nivalis.Melee.Events;

[Serializable, NetSerializable]
public sealed class NivalisLightAttackEvent : NivalisAttackEvent
{
    public readonly NetEntity? Target;
    public readonly NetEntity Weapon;

    public NivalisLightAttackEvent(NetEntity? target, NetEntity weapon, NetCoordinates coordinates)
        : base(coordinates)
    {
        Target = target;
        Weapon = weapon;
    }
}
