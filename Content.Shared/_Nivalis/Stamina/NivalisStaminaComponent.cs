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
    public float SprintDrain = 15f;

    [DataField, AutoNetworkedField, ViewVariables(VVAccess.ReadWrite)]
    public float RecoveryRate = 12f;

    [DataField, AutoNetworkedField, ViewVariables(VVAccess.ReadWrite)]
    public float ExhaustionThreshold = 0.2f;

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoPausedField]
    public TimeSpan NextStaminaTick = TimeSpan.Zero;

    [DataField, AutoNetworkedField, ViewVariables(VVAccess.ReadWrite)]
    public bool Exhausted;
}
