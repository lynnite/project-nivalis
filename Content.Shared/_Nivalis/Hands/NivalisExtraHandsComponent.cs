using Content.Shared.Hands.Components;
using Robust.Shared.GameStates;

namespace Content.Shared._Nivalis.Hands;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true)]
[Access(typeof(SharedNivalisHandsSystem))]
public sealed partial class NivalisExtraHandsComponent : Component
{
    [DataField]
    [AutoNetworkedField]
    public int Hands = 6;

    [DataField]
    public int MaxTotalHands = 10;

    [DataField]
    [AutoNetworkedField]
    public bool Used;

    [DataField]
    public HandLocation Location = HandLocation.Middle;
}
