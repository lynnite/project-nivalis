using Robust.Shared.GameStates;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared._Nivalis.Perks;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, AutoGenerateComponentPause]
public sealed partial class NivalisRiskrunnerComponent : Component
{
    [ViewVariables(VVAccess.ReadOnly)]
    public EntityUid? ActiveWeapon;

    [ViewVariables(VVAccess.ReadOnly)]
    public string? ActiveHand;

    [DataField, AutoNetworkedField, ViewVariables(VVAccess.ReadWrite)]
    public float Charge = 100f;

    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public float SummonThreshold = 50f;

    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public float RechargeRate = 8f;

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoPausedField, ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan ExpiresAt;

    [ViewVariables(VVAccess.ReadOnly)]
    public bool HasDuration;
}
