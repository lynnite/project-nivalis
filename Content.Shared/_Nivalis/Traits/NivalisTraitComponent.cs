using Content.Shared._Nivalis.Traits;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._Nivalis.Traits;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true), Access(typeof(SharedNivalisTraitSystem))]
public sealed partial class NivalisTraitComponent : Component
{
    [DataField, AutoNetworkedField, ViewVariables(VVAccess.ReadWrite)]
    public List<ProtoId<NivalisTraitPrototype>> Traits = new();

    [DataField, AutoNetworkedField, ViewVariables(VVAccess.ReadWrite)]
    public int MaxTraits = 3;

    [ViewVariables]
    public float DamageDealtMult = 1f;
    [ViewVariables]
    public float DamageTakenMult = 1f;
    [ViewVariables]
    public float SpeedMult = 1f;
    [ViewVariables]
    public float HungerDecayMult = 1f;
    [ViewVariables]
    public float ThirstDecayMult = 1f;
    [ViewVariables]
    public float StaminaDrainMult = 1f;
    [ViewVariables]
    public float MoralePenaltyReduction = 0f;
}
