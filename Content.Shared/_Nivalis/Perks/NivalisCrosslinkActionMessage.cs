using System.Numerics;
using Robust.Shared.Map;
using Robust.Shared.Serialization;

namespace Content.Shared._Nivalis.Perks;

[Serializable, NetSerializable]
public sealed class NivalisCrosslinkActionMessage : EntityEventArgs
{
    public readonly NivalisCrosslinkAction Action;

    public readonly NetCoordinates Coordinates;

    public readonly Vector2 AimDirection;

    public NivalisCrosslinkActionMessage(NivalisCrosslinkAction action, NetCoordinates coordinates, Vector2 aimDirection)
    {
        Action = action;
        Coordinates = coordinates;
        AimDirection = aimDirection;
    }
}

[Serializable, NetSerializable]
public enum NivalisCrosslinkAction : byte
{
    PlaceKnife,

    RecallKnife,
}
