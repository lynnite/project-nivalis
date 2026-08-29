using Robust.Shared.GameStates;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared._Nivalis.Stamina;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true), AutoGenerateComponentPause]
public sealed partial class NivalisStaminaComponent : Component
{
    [DataField, AutoNetworkedField, ViewVariables(VVAccess.ReadWrite)]
    public float Current = 100f;

    [DataField, AutoNetworkedField, ViewVariables(VVAccess.ReadWrite)]
    public float Max = 100f;

    [DataField, AutoNetworkedField, ViewVariables(VVAccess.ReadWrite)]
    public float ShoveCost = 25f;

    [DataField, AutoNetworkedField, ViewVariables(VVAccess.ReadWrite)]
    public float RecoveryRate = 40f;

    [DataField, AutoNetworkedField, ViewVariables(VVAccess.ReadWrite)]
    public float Exhaustion;

    [DataField, AutoNetworkedField, ViewVariables(VVAccess.ReadWrite)]
    public float ExhaustionMax = 200f;

    [DataField, AutoNetworkedField, ViewVariables(VVAccess.ReadWrite)]
    public float SprintExhaustionDrain = 10f;

    [DataField, AutoNetworkedField, ViewVariables(VVAccess.ReadWrite)]
    public float ExhaustionRecoveryRate = 25f;

    [DataField, AutoNetworkedField, ViewVariables(VVAccess.ReadWrite)]
    public float RegenDelay = 3f;

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoPausedField]
    public TimeSpan LastExertion = TimeSpan.Zero;

    [DataField, AutoNetworkedField, ViewVariables(VVAccess.ReadWrite)]
    public bool Exhausted;
}
