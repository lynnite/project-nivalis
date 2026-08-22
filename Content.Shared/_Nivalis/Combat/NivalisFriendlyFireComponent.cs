using Robust.Shared.GameStates;

namespace Content.Shared._Nivalis.Combat;

[RegisterComponent, NetworkedComponent]
public sealed partial class NivalisFriendlyFireComponent : Component
{
    [DataField]
    public NivalisCombatTeam Team = NivalisCombatTeam.None;
}
