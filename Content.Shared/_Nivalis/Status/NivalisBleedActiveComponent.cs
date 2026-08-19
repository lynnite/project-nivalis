using Robust.Shared.GameStates;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared._Nivalis.Status;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentPause]
public sealed partial class NivalisBleedActiveComponent : Component
{
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public float DamagePerSecond = 1f;

    [DataField]
    public EntityUid? Source;

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoPausedField]
    public TimeSpan NextTick;
}

