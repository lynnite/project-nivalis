using Content.Shared.Weapons.Ranged.Components;
using Robust.Shared.GameStates;

namespace Content.Shared._Nivalis.Weapons;

/// <summary>
///     An ammo provider for Nivalis guns that draws ammunition from the shooter's
///     <see cref="NivalisAmmoPoolComponent"/> instead of physical magazines or batteries.
///     Must be placed on an entity that also has a <see cref="NivalisGunComponent"/> which
///     declares the ammo type and capacity.
/// </summary>
/// <remarks>
///     Handles <see cref="TakeAmmoEvent"/> and <see cref="GetAmmoCountEvent"/> so the
///     standard predicted gun firing pipeline works unchanged. The gun spawns the hitscan
///     projectile declared on <see cref="NivalisGunComponent.Projectile"/>.
/// </remarks>
[RegisterComponent, NetworkedComponent]
public sealed partial class NivalisPoolAmmoProviderComponent : AmmoProviderComponent;
