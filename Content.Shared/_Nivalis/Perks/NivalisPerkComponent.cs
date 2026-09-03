using Content.Shared._Nivalis.Perks;

using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._Nivalis.Perks;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true), Access(typeof(SharedNivalisPerkSystem))]
public sealed partial class NivalisPerkComponent : Component
{
    [DataField, AutoNetworkedField, ViewVariables(VVAccess.ReadWrite)]
    public ProtoId<NivalisPerkPrototype>? Perk;

    [ViewVariables] public float DamageDealtMult = 1f;
    [ViewVariables] public float DamageTakenMult = 1f;
    [ViewVariables] public float SpeedMult = 1f;
    [ViewVariables] public float HungerDecayMult = 1f;
    [ViewVariables] public float ThirstDecayMult = 1f;
    [ViewVariables] public float StaminaDrainMult = 1f;
    [ViewVariables] public float MoralePenaltyReduction = 0f;

    [ViewVariables] public float LightSwingIntervalMult = 1f;
    [ViewVariables] public float HeavySwingIntervalMult = 1f;
    [ViewVariables] public float MeleeDamageMult = 1f;
    [ViewVariables] public float FistDamageMult = 1f;
    [ViewVariables] public float ShoveCooldownMult = 1f;
    [ViewVariables] public float SemiAutoFireRateMult = 1f;
    [ViewVariables] public float FanningFireRateMult = 1f;
    [ViewVariables] public float RecoilMult = 1f;
    [ViewVariables] public float HipFireSpreadMult = 1f;
    [ViewVariables] public float ReloadDelayMult = 1f;
    [ViewVariables] public float AimSpeedMult = 1f;
    [ViewVariables] public float FirearmExplosiveDamageTakenMult = 1f;

    [ViewVariables] public float MaxHealthBonus = 0f;
    [ViewVariables] public float MaxStaminaBonus = 0f;
    [ViewVariables] public float StaminaRegenMult = 1f;
    [ViewVariables] public float HealthRegenPerTick = 0f;
    [ViewVariables] public float HealthRegenInterval = 0f;

    [ViewVariables] public float TrapDeploySpeedMult = 1f;
    [ViewVariables] public float AttackSpeedMult = 1f;
    [ViewVariables] public float AmmoScavengeMult = 1f;
    [ViewVariables] public float ExplosiveDamageMult = 1f;
    [ViewVariables] public float ShoveSpeedMult = 1f;
    [ViewVariables] public float HarvestMult = 1f;
    [ViewVariables] public float CraftingMult = 1f;
    [ViewVariables] public float DefenseMult = 1f;
    [ViewVariables] public float MeleeDamageCap = 0f;

    [DataField, AutoNetworkedField, ViewVariables(VVAccess.ReadWrite)]
    public bool ImmuneToBleed;
    [DataField, AutoNetworkedField, ViewVariables(VVAccess.ReadWrite)]
    public bool ImmuneToBurn;
    [DataField, AutoNetworkedField, ViewVariables(VVAccess.ReadWrite)]
    public bool ImmuneToCripple;
    [DataField, AutoNetworkedField, ViewVariables(VVAccess.ReadWrite)]
    public bool ImmuneToFracture;
    [DataField, AutoNetworkedField, ViewVariables(VVAccess.ReadWrite)]
    public bool ImmuneToGrapple;
    [DataField, AutoNetworkedField, ViewVariables(VVAccess.ReadWrite)]
    public bool ImmuneToExplosive;
    [DataField, AutoNetworkedField, ViewVariables(VVAccess.ReadWrite)]
    public bool ImmuneToSickness;
    [DataField, AutoNetworkedField, ViewVariables(VVAccess.ReadWrite)]
    public bool UnaffectedByMorale;
    [DataField, AutoNetworkedField, ViewVariables(VVAccess.ReadWrite)]
    public bool CanAimRanged = true;
    [DataField, AutoNetworkedField, ViewVariables(VVAccess.ReadWrite)]
    public bool CanShoveSpecial;
    [DataField, AutoNetworkedField, ViewVariables(VVAccess.ReadWrite)]
    public bool NoFallDamage;
}
