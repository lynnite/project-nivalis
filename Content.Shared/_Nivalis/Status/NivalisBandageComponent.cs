using Robust.Shared.GameStates;

namespace Content.Shared._Nivalis.Status;

[RegisterComponent, NetworkedComponent]
public sealed partial class NivalisBandageComponent : Component
{
    [DataField]
    public bool Aseptic;
}
