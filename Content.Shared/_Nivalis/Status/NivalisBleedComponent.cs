using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._Nivalis.Status;

/// <summary>
///     When placed on a melee weapon, every successful hit against a living mob
///     applies the Nivalis bleed status effect to the victim.
/// </summary>
[RegisterComponent, NetworkedComponent]
[EntityCategory("Weapons")]
public sealed partial class NivalisBleedComponent : Component
{
    /// <summary>
    ///     Brute damage dealt per second while bleeding.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public float DamagePerSecond = 1f;

    /// <summary>
    ///     Duration of the bleed effect in seconds.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public float Duration = 20f;
}
