using Robust.Shared.GameStates;

namespace Content.Shared._Nivalis.Status;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class NivalisFractureComponent : Component
{
    [DataField, AutoNetworkedField]
    public bool ArmFractured;

    [DataField, AutoNetworkedField]
    public bool LegFractured;
}
