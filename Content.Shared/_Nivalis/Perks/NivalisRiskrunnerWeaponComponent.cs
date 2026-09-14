using Robust.Shared.GameStates;

namespace Content.Shared._Nivalis.Perks;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class NivalisRiskrunnerWeaponComponent : Component
{
    [DataField, AutoNetworkedField]
    public new EntityUid? Owner;

    [DataField, AutoNetworkedField]
    public float ShotCost = 1f;

    [DataField, AutoNetworkedField]
    public float MaxCharge = 100f;
}
