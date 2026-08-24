using System.Numerics;
using Robust.Shared.GameStates;
using Robust.Shared.Map;

namespace Content.Shared._Nivalis.Weapons;

/// <summary>
///     Placed on a bullet-trail visual entity so it stretches from the firer's muzzle out to the
///     tracked <see cref="Target"/> projectile's current position every tick. The trail despawns
///     automatically once the projectile is deleted, so it always ends exactly at the bullet.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class NivalisTrailComponent : Component
{
    /// <summary>
    ///     The projectile this trail is attached to.
    /// </summary>
    [DataField]
    public EntityUid Target;

    /// <summary>
    ///     World-space muzzle coordinate the trail stays anchored to.
    /// </summary>
    [DataField]
    public MapCoordinates Muzzle;
}
