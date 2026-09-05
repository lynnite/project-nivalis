using Robust.Shared.GameStates;
using Robust.Shared.GameObjects;

namespace Content.Shared._Nivalis.Scrap;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true), Access(typeof(SharedNivalisScrapSystem))]
public sealed partial class NivalisScrapComponent : Component
{
    [DataField, AutoNetworkedField, ViewVariables(VVAccess.ReadWrite)]
    public float Scrap = 0f;
}
