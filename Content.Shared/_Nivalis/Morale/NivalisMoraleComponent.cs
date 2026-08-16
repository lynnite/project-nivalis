using Robust.Shared.GameStates;

namespace Content.Shared._Nivalis.Morale;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true)]
public sealed partial class NivalisMoraleComponent : Component
{
    [DataField, AutoNetworkedField, ViewVariables(VVAccess.ReadWrite)]
    public float Morale = 100f;

    [DataField, AutoNetworkedField, ViewVariables(VVAccess.ReadWrite)]
    public float MaxMorale = 100f;

    [DataField, AutoNetworkedField, ViewVariables(VVAccess.ReadWrite)]
    public float DeathPenalty = 20f;

    [DataField, AutoNetworkedField, ViewVariables(VVAccess.ReadWrite)]
    public NivalisMoraleLevel Level = NivalisMoraleLevel.High;
}
