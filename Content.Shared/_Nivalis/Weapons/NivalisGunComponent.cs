using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._Nivalis.Weapons;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class NivalisGunComponent : Component
{
    [DataField, AutoNetworkedField]
    public NivalisAmmoType AmmoType = NivalisAmmoType.Light;

    [DataField(required: true), AutoNetworkedField]
    public EntProtoId Projectile = default!;

    [DataField, AutoNetworkedField]
    public int MaxAmmo = 30;

    [DataField, AutoNetworkedField]
    public int MagazineCount = -1;

    [DataField, AutoNetworkedField]
    public bool Reloading;

    [DataField, AutoNetworkedField]
    public bool UsesPoolAmmo = true;

    [DataField, AutoNetworkedField]
    public int ReloadAmount = 0;
}

