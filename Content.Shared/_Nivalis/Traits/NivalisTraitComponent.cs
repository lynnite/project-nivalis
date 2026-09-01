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

    [ViewVariables]
    public float LightSwingIntervalMult = 1f;
    [ViewVariables]
    public float HeavySwingIntervalMult = 1f;
    [ViewVariables]
    public float MeleeDamageMult = 1f;
    [ViewVariables]
    public float FistDamageMult = 1f;
    [ViewVariables]
    public float ShoveCooldownMult = 1f;

    [ViewVariables]
    public float SemiAutoFireRateMult = 1f;
    [ViewVariables]
    public float FanningFireRateMult = 1f;
    [ViewVariables]
    public float RecoilMult = 1f;
    [ViewVariables]
    public float HipFireSpreadMult = 1f;
    [ViewVariables]
    public float ReloadDelayMult = 1f;
    [ViewVariables]
    public float AimSpeedMult = 1f;
    [ViewVariables]
    public float FirearmExplosiveDamageTakenMult = 1f;

    [ViewVariables]
    public float MaxHealthBonus = 0f;
    [ViewVariables]
    public float MaxStaminaBonus = 0f;
    [ViewVariables]
    public float StaminaRegenMult = 1f;
    [ViewVariables]
    public float HealthRegenPerTick = 0f;
    [ViewVariables]
    public float HealthRegenInterval = 0f;
}

