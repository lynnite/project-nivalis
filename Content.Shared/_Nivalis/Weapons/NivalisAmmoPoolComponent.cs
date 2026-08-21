using System.Collections.Generic;
using Robust.Shared.GameStates;
using Robust.Shared.GameStates;

namespace Content.Shared._Nivalis.Weapons;

/// <summary>
///     Hosted on a player's entity. Acts as the global ammo pool from which all
///     Nivalis guns draw ammunition. The HUD/gun shows counts from this pool.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class NivalisAmmoPoolComponent : Component
{
    /// <summary>
    ///     The amount of a given ammo type that is added to the pool when an
    ///     ammo box of that type is picked up. Individual ammo boxes may override this.
    /// </summary>
    [DataField, AutoNetworkedField]
    public int AmmoPickupAmount = 12;

    /// <summary>
    ///     Per-type current ammo counts in the pool.
    /// </summary>
    [DataField, AutoNetworkedField]
    public Dictionary<NivalisAmmoType, int> Ammo = new();

    /// <summary>
    ///     Starting ammo granted to the pool when the component first maps in.
    /// </summary>
    [DataField]
    public Dictionary<NivalisAmmoType, int> StartingAmmo = new()
    {
        [NivalisAmmoType.Light] = 48,
        [NivalisAmmoType.Short] = 60,
        [NivalisAmmoType.Long] = 40,
        [NivalisAmmoType.Small] = 48,
        [NivalisAmmoType.Shell] = 16,
        [NivalisAmmoType.Medium] = 40,
        [NivalisAmmoType.Heavy] = 12,
    };

    /// <summary>
    ///     Whether the starting ammo has already been applied.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool Seeded;
    /// <summary>
    ///     Helper to get the current count for a type, defaulting to zero.
    /// </summary>
    public int GetAmmo(NivalisAmmoType type)
    {
        return Ammo.TryGetValue(type, out var count) ? count : 0;
    }

    /// <summary>
    ///     Helper to set the count for a type.
    /// </summary>
    public void SetAmmo(NivalisAmmoType type, int count)
    {
        if (count <= 0)
            Ammo.Remove(type);
        else
            Ammo[type] = count;
    }
}

