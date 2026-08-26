using Content.Shared.Damage.Prototypes;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared._Nivalis.GameTicking.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true), AutoGenerateComponentPause]
public sealed partial class NivalisStormZoneComponent : Component
{
    [DataField, AutoNetworkedField, ViewVariables(VVAccess.ReadWrite)]
    public bool Active;

    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public float Radius = 12f;

    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public float StormDamagePerSecond = 5f;

    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public float DamageInterval = 1f;

    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public ProtoId<DamageTypePrototype> DamageType = "Storm";

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoPausedField]
    public TimeSpan NextTick = TimeSpan.Zero;
}

