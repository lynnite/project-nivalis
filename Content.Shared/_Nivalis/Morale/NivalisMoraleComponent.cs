using Robust.Shared.GameStates;

namespace Content.Shared._Nivalis.Morale;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true)]
public sealed partial class NivalisMoraleComponent : Component
{
    [DataField, AutoNetworkedField, ViewVariables(VVAccess.ReadWrite)]
    public int Morale = 0;

    [DataField, AutoNetworkedField, ViewVariables(VVAccess.ReadWrite)]
    public int MaxMorale = 4;

    [DataField, AutoNetworkedField, ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan NextResetTime;
}
