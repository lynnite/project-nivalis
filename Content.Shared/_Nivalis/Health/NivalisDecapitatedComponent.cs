using Robust.Shared.GameStates;

namespace Content.Shared._Nivalis.Health;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(raiseAfterAutoHandleState: true)]
public sealed partial class NivalisDecapitatedComponent : Component
{
    [DataField, AutoNetworkedField]
    public float SpriteOffset;
}
