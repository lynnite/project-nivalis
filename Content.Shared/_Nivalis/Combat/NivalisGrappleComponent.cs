using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared._Nivalis.Combat;

[RegisterComponent, NetworkedComponent]
public sealed partial class NivalisGrappleComponent : Component
{
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public float Range = 2.25f;

    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public float GrappleTime = 1f;

    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public float StunTime = 1f;

    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public float Cooldown = 8f;

    [DataField]
    public TimeSpan NextAttempt;
}
