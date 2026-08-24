using Content.Shared.Damage.Prototypes;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._Nivalis.Status;

[RegisterComponent, NetworkedComponent]
public sealed partial class NivalisNoBleedComponent : Component
{
    [DataField]
    public ProtoId<DamageModifierSetPrototype> OriginalBleedModifiers = "BloodlossHuman";
}

