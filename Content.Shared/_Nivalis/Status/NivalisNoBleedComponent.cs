using Content.Shared.Damage.Prototypes;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._Nivalis.Status;

/// <summary>
///     Temporarily marks a mob as not bleeding normally. While present, the standard
///     BloodstreamSystem damage-to-bleed conversion is suppressed (its
///     <c>DamageBleedModifiers</c> are blanked out), so scavenger weapon hits only cause the
///     constant Nivalis status-effect bleed instead of the regular blood-pool bleed.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentPause]
public sealed partial class NivalisNoBleedComponent : Component
{
    /// <summary>
    ///     The <c>DamageBleedModifiers</c> id to restore when this component is removed.
    /// </summary>
    [DataField]
    public ProtoId<DamageModifierSetPrototype> OriginalBleedModifiers = "BloodlossHuman";

    /// <summary>
    ///     When this component should be removed and the original bleed modifiers restored.
    /// </summary>
    [DataField]
    public TimeSpan RemoveAt;
}

