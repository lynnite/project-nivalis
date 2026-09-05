using System.Numerics;
using Robust.Shared.Serialization;

namespace Content.Shared._Nivalis.Perks;

[Serializable, NetSerializable]
public sealed class NivalisPerkAbilityPressedMessage : EntityEventArgs
{
    public readonly bool Holding;

    public readonly Vector2 AimDirection;

    public NivalisPerkAbilityPressedMessage(bool holding, Vector2 aimDirection)
    {
        Holding = holding;
        AimDirection = aimDirection;
    }
}
