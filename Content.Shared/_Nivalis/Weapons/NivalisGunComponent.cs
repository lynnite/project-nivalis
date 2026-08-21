using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._Nivalis.Weapons;

/// <summary>
///     Marks a gun as a Nivalis gun. It draws ammunition from the shooter's
///     <see cref="NivalisAmmoPoolComponent"/> instead of physical magazines.
///     The <see cref="AmmoType"/> determines which pool entry is drained.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class NivalisGunComponent : Component
{
    /// <summary>
    ///     The ammo type this gun consumes from the player's ammo pool.
    /// </summary>
    [DataField, AutoNetworkedField]
    public NivalisAmmoType AmmoType = NivalisAmmoType.Light;

    /// <summary>
    ///     The projectile/hitscan prototype fired by this gun. This is the equivalent
    ///     of a laser gun's <c>proto</c> on <c>BatteryAmmoProvider</c>.
    /// </summary>
    [DataField(required: true), AutoNetworkedField]
    public EntProtoId Projectile = default!;

    /// <summary>
    ///     The maximum number of shots this gun's magazine can hold before it needs a
    ///     reload. This is the "capacity" / denominator shown by the ammo counter.
    /// </summary>
    [DataField, AutoNetworkedField]
    public int MaxAmmo = 30;

    [DataField, AutoNetworkedField]
    public int MagazineCount = -1;

    [DataField, AutoNetworkedField]
    public bool Reloading;

    /// <summary>
    ///     Whether the gun uses the Nivalis pool ammo provider. When false the gun
    ///     falls back to standard ammo handling (used for transition/tooling).
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool UsesPoolAmmo = true;
}

