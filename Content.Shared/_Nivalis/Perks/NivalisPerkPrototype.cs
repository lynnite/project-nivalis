using Robust.Shared.Prototypes;


namespace Content.Shared._Nivalis.Perks;

/// <summary>
///     A single selectable survivor perk. Perks stack a payload of passive
///     modifiers on top of (and independently from) chosen Trait modifiers.
///     The remaining unique "active ability" and special rules for a perk are
///     driven separately by server logic keyed on the prototype ID.
/// </summary>
[Prototype]
public sealed partial class NivalisPerkPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = string.Empty;

    [DataField]
    public LocId Name = string.Empty;

    [DataField]
    public LocId Description = string.Empty;

    [DataField] public float DamageDealtMult = 1f;
    [DataField] public float DamageTakenMult = 1f;
    [DataField] public float SpeedMult = 1f;
    [DataField] public float HungerDecayMult = 1f;
    [DataField] public float ThirstDecayMult = 1f;
    [DataField] public float StaminaDrainMult = 1f;
    [DataField] public float MoralePenaltyReduction = 0f;

    [DataField] public float LightSwingIntervalMult = 1f;
    [DataField] public float HeavySwingIntervalMult = 1f;
    [DataField] public float MeleeDamageMult = 1f;
    [DataField] public float FistDamageMult = 1f;
    [DataField] public float ShoveCooldownMult = 1f;
    [DataField] public float SemiAutoFireRateMult = 1f;
    [DataField] public float FanningFireRateMult = 1f;
    [DataField] public float RecoilMult = 1f;
    [DataField] public float HipFireSpreadMult = 1f;
    [DataField] public float ReloadDelayMult = 1f;
    [DataField] public float AimSpeedMult = 1f;
    [DataField] public float FirearmExplosiveDamageTakenMult = 1f;

    [DataField] public float MaxHealthBonus = 0f;
    [DataField] public float MaxStaminaBonus = 0f;
    [DataField] public float StaminaRegenMult = 1f;
    [DataField] public float HealthRegenPerTick = 0f;
    [DataField] public float HealthRegenInterval = 0f;

    [DataField] public float TrapDeploySpeedMult = 1f;
    [DataField] public float AttackSpeedMult = 1f;
    [DataField] public float AmmoScavengeMult = 1f;
    [DataField] public float ExplosiveDamageMult = 1f;
    [DataField] public float ShoveSpeedMult = 1f;
    [DataField] public float HarvestMult = 1f;
    [DataField] public float CraftingMult = 1f;
    [DataField] public float DefenseMult = 1f;
    [DataField] public float MeleeDamageCap = 0f;

    [DataField] public bool ImmuneToBleed;
    [DataField] public bool ImmuneToBurn;
    [DataField] public bool ImmuneToCripple;
    [DataField] public bool ImmuneToFracture;
    [DataField] public bool ImmuneToGrapple;
    [DataField] public bool ImmuneToExplosive;
    [DataField] public bool ImmuneToSickness;
    [DataField] public bool UnaffectedByMorale;
    [DataField] public bool CanAimRanged = true;
    [DataField] public bool NoFallDamage;
}

