using Robust.Shared.Serialization;

namespace Content.Shared._Nivalis.Weapons;

/// <summary>
///     Predictive input event raised when the player presses/releases the aim key
///     (right mouse button) while holding a Nivalis aimable gun.
/// </summary>
[Serializable, NetSerializable]
public sealed class NivalisAimEvent : EntityEventArgs
{
    public readonly bool Active;

    public NivalisAimEvent(bool active)
    {
        Active = active;
    }
}
