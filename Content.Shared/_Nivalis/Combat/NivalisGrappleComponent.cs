using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared._Nivalis.Combat;

/// <summary>
///     Added to NPCs that are able to "grapple": they lock onto a nearby target for
///     <see cref="GrappleTime"/> seconds (via a do-after), then stun them for
///     <see cref="StunTime"/>.
/// </summary>
[RegisterComponent, NetworkedComponent]
[EntityCategory("Mobs")]
public sealed partial class NivalisGrappleComponent : Component
{
    /// <summary>
    ///     Maximum distance from which the NPC may initiate a grapple.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public float Range = 2.25f;

    /// <summary>
    ///     How long the grapple do-after takes before the stun applies.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public float GrappleTime = 1f;

    /// <summary>
    ///     How long the victim is stunned for once caught.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public float StunTime = 1f;

    /// <summary>
    ///     Cooldown between grapple attempts.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public float Cooldown = 8f;

    /// <summary>
    ///     The next time an attempt may be made.
    /// </summary>
    [DataField]
    public TimeSpan NextAttempt;
}
