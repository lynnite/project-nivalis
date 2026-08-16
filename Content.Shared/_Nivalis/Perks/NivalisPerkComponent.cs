using Content.Shared._Nivalis.Perks;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._Nivalis.Perks;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true), Access(typeof(SharedNivalisPerkSystem))]
public sealed partial class NivalisPerkComponent : Component
{
    [DataField, AutoNetworkedField, ViewVariables(VVAccess.ReadWrite)]
    public List<ProtoId<NivalisPerkPrototype>> Perks = new();

    /// <summary>
    ///     Maximum number of perks a survivor may hold
    /// </summary>
    [DataField, AutoNetworkedField, ViewVariables(VVAccess.ReadWrite)]
    public int MaxPerks = 5;

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

