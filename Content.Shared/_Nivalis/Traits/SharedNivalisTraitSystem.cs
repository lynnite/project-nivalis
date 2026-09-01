using Robust.Shared.Prototypes;

namespace Content.Shared._Nivalis.Traits;

public abstract partial class SharedNivalisTraitSystem : EntitySystem
{
    [Dependency] private IPrototypeManager _proto = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<NivalisTraitComponent, MapInitEvent>(OnMapInit);
    }

    private void OnMapInit(Entity<NivalisTraitComponent> ent, ref MapInitEvent args)
    {
        Recalculate(ent);
        Dirty(ent);
    }

    public bool Recalculate(Entity<NivalisTraitComponent> ent)
    {
        var oldDealt = ent.Comp.DamageDealtMult;
        var oldTaken = ent.Comp.DamageTakenMult;
        var oldSpeed = ent.Comp.SpeedMult;
        var oldHunger = ent.Comp.HungerDecayMult;
        var oldThirst = ent.Comp.ThirstDecayMult;
        var oldStamina = ent.Comp.StaminaDrainMult;
        var oldMorale = ent.Comp.MoralePenaltyReduction;

        var oldLightSwing = ent.Comp.LightSwingIntervalMult;
        var oldHeavySwing = ent.Comp.HeavySwingIntervalMult;
        var oldMeleeDamage = ent.Comp.MeleeDamageMult;
        var oldFistDamage = ent.Comp.FistDamageMult;
        var oldShoveCooldown = ent.Comp.ShoveCooldownMult;
        var oldSemiAutoRate = ent.Comp.SemiAutoFireRateMult;
        var oldFanningRate = ent.Comp.FanningFireRateMult;
        var oldRecoil = ent.Comp.RecoilMult;
        var oldHipFire = ent.Comp.HipFireSpreadMult;
        var oldReloadDelay = ent.Comp.ReloadDelayMult;
        var oldAimSpeed = ent.Comp.AimSpeedMult;
        var oldFirearmTaken = ent.Comp.FirearmExplosiveDamageTakenMult;
        var oldMaxHealth = ent.Comp.MaxHealthBonus;
        var oldMaxStamina = ent.Comp.MaxStaminaBonus;
        var oldStaminaRegen = ent.Comp.StaminaRegenMult;
        var oldHealthRegen = ent.Comp.HealthRegenPerTick;
        var oldHealthRegenInterval = ent.Comp.HealthRegenInterval;

        ent.Comp.DamageDealtMult = 1f;
        ent.Comp.DamageTakenMult = 1f;
        ent.Comp.SpeedMult = 1f;
        ent.Comp.HungerDecayMult = 1f;
        ent.Comp.ThirstDecayMult = 1f;
        ent.Comp.StaminaDrainMult = 1f;
        ent.Comp.MoralePenaltyReduction = 0f;

        ent.Comp.LightSwingIntervalMult = 1f;
        ent.Comp.HeavySwingIntervalMult = 1f;
        ent.Comp.MeleeDamageMult = 1f;
        ent.Comp.FistDamageMult = 1f;
        ent.Comp.ShoveCooldownMult = 1f;
        ent.Comp.SemiAutoFireRateMult = 1f;
        ent.Comp.FanningFireRateMult = 1f;
        ent.Comp.RecoilMult = 1f;
        ent.Comp.HipFireSpreadMult = 1f;
        ent.Comp.ReloadDelayMult = 1f;
        ent.Comp.AimSpeedMult = 1f;
        ent.Comp.FirearmExplosiveDamageTakenMult = 1f;
        ent.Comp.MaxHealthBonus = 0f;
        ent.Comp.MaxStaminaBonus = 0f;
        ent.Comp.StaminaRegenMult = 1f;
        ent.Comp.HealthRegenPerTick = 0f;
        ent.Comp.HealthRegenInterval = 0f;

        foreach (var traitId in ent.Comp.Traits)
        {
            if (!_proto.TryIndex<NivalisTraitPrototype>(traitId, out var trait))
            {
                Log.Error($"Unknown Nivalis trait '{traitId}' on {ToPrettyString(ent.Owner)}");
                continue;
            }

            ent.Comp.DamageDealtMult *= trait.DamageDealtMult;
            ent.Comp.DamageTakenMult *= trait.DamageTakenMult;
            ent.Comp.SpeedMult *= trait.SpeedMult;
            ent.Comp.HungerDecayMult *= trait.HungerDecayMult;
            ent.Comp.ThirstDecayMult *= trait.ThirstDecayMult;
            ent.Comp.StaminaDrainMult *= trait.StaminaDrainMult;
            ent.Comp.MoralePenaltyReduction += trait.MoralePenaltyReduction;

            ent.Comp.LightSwingIntervalMult *= trait.LightSwingIntervalMult;
            ent.Comp.HeavySwingIntervalMult *= trait.HeavySwingIntervalMult;
            ent.Comp.MeleeDamageMult *= trait.MeleeDamageMult;
            ent.Comp.FistDamageMult *= trait.FistDamageMult;
            ent.Comp.ShoveCooldownMult *= trait.ShoveCooldownMult;
            ent.Comp.SemiAutoFireRateMult *= trait.SemiAutoFireRateMult;
            ent.Comp.FanningFireRateMult *= trait.FanningFireRateMult;
            ent.Comp.RecoilMult *= trait.RecoilMult;
            ent.Comp.HipFireSpreadMult *= trait.HipFireSpreadMult;
            ent.Comp.ReloadDelayMult *= trait.ReloadDelayMult;
            ent.Comp.AimSpeedMult *= trait.AimSpeedMult;
            ent.Comp.FirearmExplosiveDamageTakenMult *= trait.FirearmExplosiveDamageTakenMult;
            ent.Comp.MaxHealthBonus += trait.MaxHealthBonus;
            ent.Comp.MaxStaminaBonus += trait.MaxStaminaBonus;
            ent.Comp.StaminaRegenMult *= trait.StaminaRegenMult;
            ent.Comp.HealthRegenPerTick += trait.HealthRegenPerTick;
            ent.Comp.HealthRegenInterval += trait.HealthRegenInterval;
        }

        ent.Comp.DamageDealtMult = MathF.Max(0.1f, ent.Comp.DamageDealtMult);
        ent.Comp.DamageTakenMult = MathF.Max(0.1f, ent.Comp.DamageTakenMult);
        ent.Comp.SpeedMult = MathF.Max(0.5f, ent.Comp.SpeedMult);
        ent.Comp.HungerDecayMult = MathF.Max(0.1f, ent.Comp.HungerDecayMult);
        ent.Comp.ThirstDecayMult = MathF.Max(0.1f, ent.Comp.ThirstDecayMult);
        ent.Comp.StaminaDrainMult = MathF.Max(0.1f, ent.Comp.StaminaDrainMult);
        ent.Comp.MoralePenaltyReduction = Math.Clamp(ent.Comp.MoralePenaltyReduction, 0f, 1f);

        ent.Comp.LightSwingIntervalMult = MathF.Max(0.1f, ent.Comp.LightSwingIntervalMult);
        ent.Comp.HeavySwingIntervalMult = MathF.Max(0.1f, ent.Comp.HeavySwingIntervalMult);
        ent.Comp.MeleeDamageMult = MathF.Max(0.1f, ent.Comp.MeleeDamageMult);
        ent.Comp.FistDamageMult = MathF.Max(0.1f, ent.Comp.FistDamageMult);
        ent.Comp.ShoveCooldownMult = MathF.Max(0.1f, ent.Comp.ShoveCooldownMult);
        ent.Comp.SemiAutoFireRateMult = MathF.Max(0.1f, ent.Comp.SemiAutoFireRateMult);
        ent.Comp.FanningFireRateMult = MathF.Max(0.1f, ent.Comp.FanningFireRateMult);
        ent.Comp.RecoilMult = MathF.Max(0.01f, ent.Comp.RecoilMult);
        ent.Comp.HipFireSpreadMult = MathF.Max(0.1f, ent.Comp.HipFireSpreadMult);
        ent.Comp.ReloadDelayMult = MathF.Max(0.1f, ent.Comp.ReloadDelayMult);
        ent.Comp.AimSpeedMult = MathF.Max(0.1f, ent.Comp.AimSpeedMult);
        ent.Comp.FirearmExplosiveDamageTakenMult = MathF.Max(0f, ent.Comp.FirearmExplosiveDamageTakenMult);
        ent.Comp.MaxHealthBonus = MathF.Max(0f, ent.Comp.MaxHealthBonus);
        ent.Comp.MaxStaminaBonus = MathF.Max(0f, ent.Comp.MaxStaminaBonus);
        ent.Comp.StaminaRegenMult = MathF.Max(0.1f, ent.Comp.StaminaRegenMult);

        var changed = !MathHelper.CloseTo(oldDealt, ent.Comp.DamageDealtMult)
                      || !MathHelper.CloseTo(oldTaken, ent.Comp.DamageTakenMult)
                      || !MathHelper.CloseTo(oldSpeed, ent.Comp.SpeedMult)
                      || !MathHelper.CloseTo(oldHunger, ent.Comp.HungerDecayMult)
                      || !MathHelper.CloseTo(oldThirst, ent.Comp.ThirstDecayMult)
                      || !MathHelper.CloseTo(oldStamina, ent.Comp.StaminaDrainMult)
                      || !MathHelper.CloseTo(oldMorale, ent.Comp.MoralePenaltyReduction)
                      || !MathHelper.CloseTo(oldLightSwing, ent.Comp.LightSwingIntervalMult)
                      || !MathHelper.CloseTo(oldHeavySwing, ent.Comp.HeavySwingIntervalMult)
                      || !MathHelper.CloseTo(oldMeleeDamage, ent.Comp.MeleeDamageMult)
                      || !MathHelper.CloseTo(oldFistDamage, ent.Comp.FistDamageMult)
                      || !MathHelper.CloseTo(oldShoveCooldown, ent.Comp.ShoveCooldownMult)
                      || !MathHelper.CloseTo(oldSemiAutoRate, ent.Comp.SemiAutoFireRateMult)
                      || !MathHelper.CloseTo(oldFanningRate, ent.Comp.FanningFireRateMult)
                      || !MathHelper.CloseTo(oldRecoil, ent.Comp.RecoilMult)
                      || !MathHelper.CloseTo(oldHipFire, ent.Comp.HipFireSpreadMult)
                      || !MathHelper.CloseTo(oldReloadDelay, ent.Comp.ReloadDelayMult)
                      || !MathHelper.CloseTo(oldAimSpeed, ent.Comp.AimSpeedMult)
                      || !MathHelper.CloseTo(oldFirearmTaken, ent.Comp.FirearmExplosiveDamageTakenMult)
                      || !MathHelper.CloseTo(oldMaxHealth, ent.Comp.MaxHealthBonus)
                      || !MathHelper.CloseTo(oldMaxStamina, ent.Comp.MaxStaminaBonus)
                      || !MathHelper.CloseTo(oldStaminaRegen, ent.Comp.StaminaRegenMult)
                      || !MathHelper.CloseTo(oldHealthRegen, ent.Comp.HealthRegenPerTick)
                      || !MathHelper.CloseTo(oldHealthRegenInterval, ent.Comp.HealthRegenInterval);

        return changed;
    }
}
