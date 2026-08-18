using System.Numerics;
using Robust.Shared.Map;
using Robust.Shared.Serialization;

namespace Content.Shared._Nivalis.Melee.Events;

[Serializable, NetSerializable]
public sealed class NivalisMeleeLungeEvent : EntityEventArgs
{
    public NetEntity Entity;

    public NetEntity Weapon;

    public Angle Angle;

    public Vector2 LocalPos;

    public string? Animation;

    public NivalisMeleeLungeEvent(NetEntity entity, NetEntity weapon, Angle angle, Vector2 localPos, string? animation)
    {
        Entity = entity;
        Weapon = weapon;
        Angle = angle;
        LocalPos = localPos;
        Animation = animation;
    }
}
