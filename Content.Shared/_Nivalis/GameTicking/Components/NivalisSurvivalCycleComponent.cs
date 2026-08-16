using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared._Nivalis.GameTicking.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true), AutoGenerateComponentPause]
public sealed partial class NivalisSurvivalCycleComponent : Component
{
    [DataField, AutoNetworkedField, ViewVariables(VVAccess.ReadWrite)]
    public NivalisSurvivalPhase Phase = NivalisSurvivalPhase.Scavenge;

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoPausedField]
    public TimeSpan NextPhaseChange = TimeSpan.Zero;

    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public float ScavengeDuration = 150f;

    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public float StormDuration = 600f;

    [DataField, AutoNetworkedField, ViewVariables(VVAccess.ReadWrite)]
    public int WavesCleared = 0;
}

