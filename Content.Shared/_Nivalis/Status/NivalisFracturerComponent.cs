using Robust.Shared.GameStates;

namespace Content.Shared._Nivalis.Status;

[RegisterComponent, NetworkedComponent]
public sealed partial class NivalisFracturerComponent : Component
{
    [DataField]
    public float FractureChance = 0.1f;
}
