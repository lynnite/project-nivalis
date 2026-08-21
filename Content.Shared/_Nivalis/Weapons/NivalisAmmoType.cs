using Robust.Shared.Serialization;

namespace Content.Shared._Nivalis.Weapons;

/// <summary>
///     The distinct types of ammunition used by Nivalis guns.
///     Each gun draws from the player's shared ammo pool using a single type.
/// </summary>
[Serializable, NetSerializable]
public enum NivalisAmmoType : byte
{
    /// <summary>Light handgun rounds.</summary>
    Light,

    /// <summary>Short SMG / submachine gun rounds.</summary>
    Short,

    /// <summary>Long rifle rounds.</summary>
    Long,

    /// <summary>Small pistol rounds.</summary>
    Small,

    /// <summary>Shotgun shells.</summary>
    Shell,

    /// <summary>Medium assault rifle rounds.</summary>
    Medium,

    /// <summary>Heavy machine gun / anti-materiel rounds.</summary>
    Heavy,
}
