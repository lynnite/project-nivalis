using Robust.Shared.GameStates;

namespace Content.Shared._Nivalis.Perks;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true)]
public sealed partial class NivalisBlitzerComponent : Component
{
    [DataField, AutoNetworkedField, ViewVariables(VVAccess.ReadWrite)]
    public float Charge;

    [ViewVariables(VVAccess.ReadOnly)]
    public float RechargeRate = 1.1f;

    [ViewVariables(VVAccess.ReadOnly)]
    public float BombCost = 16.67f;

    [ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan NextActionAt = TimeSpan.Zero;

    [ViewVariables(VVAccess.ReadOnly)]
    public bool Initialised;

    [ViewVariables(VVAccess.ReadOnly)]
    public List<EntityUid> Bombs = new();
}
