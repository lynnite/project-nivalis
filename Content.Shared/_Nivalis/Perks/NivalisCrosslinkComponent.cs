using Robust.Shared.GameStates;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared._Nivalis.Perks;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true), AutoGenerateComponentPause]
public sealed partial class NivalisCrosslinkComponent : Component
{
    [DataField, AutoNetworkedField, ViewVariables(VVAccess.ReadWrite)]
    public float Charge = 100f;

    [ViewVariables(VVAccess.ReadWrite)]
    public float RechargeRate = 0.67f;

    [ViewVariables(VVAccess.ReadWrite)]
    public float KnifeCost = 20f;

    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan ThrowCooldown = TimeSpan.FromSeconds(0.4);

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoPausedField, ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan NextThrowTime;

    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan RecallCooldown = TimeSpan.FromSeconds(0.6);

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoPausedField, ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan NextRecallTime;

    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public bool Initialised;

    [ViewVariables(VVAccess.ReadOnly)]
    public List<EntityUid> Knives = new();
}
