using Robust.Shared.Prototypes;

namespace Content.Shared._Nivalis.Traits;

[Prototype]
public sealed partial class NivalisTraitPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = string.Empty;

    [DataField]
    public LocId Name = string.Empty;

    [DataField]
    public LocId Description = string.Empty;

    [DataField]
    public float DamageDealtMult = 1f;

    [DataField]
    public float DamageTakenMult = 1f;

    [DataField]
    public float SpeedMult = 1f;

    [DataField]
    public float HungerDecayMult = 1f;

    [DataField]
    public float ThirstDecayMult = 1f;

    [DataField]
    public float StaminaDrainMult = 1f;

    [DataField]
    public float MoralePenaltyReduction = 0f;
}

