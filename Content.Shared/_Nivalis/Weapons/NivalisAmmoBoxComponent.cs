using Robust.Shared.GameStates;

namespace Content.Shared._Nivalis.Weapons;

/// <summary>
///     A physical ammo box that can be picked up (interacted) to add ammunition of a
///     given <see cref="NivalisAmmoType"/> to a player's <see cref="NivalisAmmoPoolComponent"/>.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class NivalisAmmoBoxComponent : Component
{
    /// <summary>
    ///     The ammo type this box contains.
    /// </summary>
    [DataField, AutoNetworkedField]
    public NivalisAmmoType AmmoType = NivalisAmmoType.Light;

    /// <summary>
    ///     How much ammo is granted on pickup. Defaults to the pool's
    ///     <see cref="NivalisAmmoPoolComponent.AmmoPickupAmount"/> when left null.
    /// </summary>
    [DataField, AutoNetworkedField]
    public int? Amount;
}
