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

    [DataField]
    public float LightSwingIntervalMult = 1f;

    [DataField]
    public float HeavySwingIntervalMult = 1f;

    [DataField]
    public float MeleeDamageMult = 1f;

    [DataField]
    public float FistDamageMult = 1f;

    [DataField]
    public float ShoveCooldownMult = 1f;

    [DataField]
    public float SemiAutoFireRateMult = 1f;

    [DataField]
    public float FanningFireRateMult = 1f;

    [DataField]
    public float RecoilMult = 1f;

    [DataField]
    public float HipFireSpreadMult = 1f;

    [DataField]
    public float ReloadDelayMult = 1f;

    [DataField]
    public float AimSpeedMult = 1f;

    [DataField]
    public float FirearmExplosiveDamageTakenMult = 1f;

    [DataField]
    public float MaxHealthBonus = 0f;

    [DataField]
    public float MaxStaminaBonus = 0f;

    [DataField]
    public float StaminaRegenMult = 1f;

    [DataField]
    public float HealthRegenPerTick = 0f;

    [DataField]
    public float HealthRegenInterval = 0f;
}
