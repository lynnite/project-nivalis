using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._Nivalis.Weapons;

/// <summary>
///     Granted to survivors/players that should have the Nivalis ammo HUD. Grants the
///     action button that opens the ammo GUI. Intended to be added alongside
///     <see cref="NivalisAmmoPoolComponent"/>.
/// </summary>
[RegisterComponent]
public sealed partial class NivalisAmmoHudComponent : Component
{
    /// <summary>
    ///     The action prototype granted when this component is added.
    /// </summary>
    [DataField]
    public EntProtoId OpenAmmoMenuAction = "ActionNivalisOpenAmmoMenu";
}

