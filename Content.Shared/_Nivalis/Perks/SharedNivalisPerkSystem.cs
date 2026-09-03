using Robust.Shared.Prototypes;

namespace Content.Shared._Nivalis.Perks;

public abstract partial class SharedNivalisPerkSystem : EntitySystem
{
    [Dependency] private IPrototypeManager _proto = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<NivalisPerkComponent, MapInitEvent>(OnMapInit);
    }

    private void OnMapInit(Entity<NivalisPerkComponent> ent, ref MapInitEvent args)
    {
        Recalculate(ent);
        Dirty(ent);
    }

    public bool Recalculate(Entity<NivalisPerkComponent> ent)
    {
        var oldSpeed = ent.Comp.SpeedMult;
        var oldTaken = ent.Comp.DamageTakenMult;
        var oldMaxHealth = ent.Comp.MaxHealthBonus;
        var oldMaxStamina = ent.Comp.MaxStaminaBonus;

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
        ent.Comp.TrapDeploySpeedMult = 1f;
        ent.Comp.AttackSpeedMult = 1f;
        ent.Comp.AmmoScavengeMult = 1f;
        ent.Comp.ExplosiveDamageMult = 1f;
        ent.Comp.ShoveSpeedMult = 1f;
        ent.Comp.HarvestMult = 1f;
        ent.Comp.CraftingMult = 1f;
        ent.Comp.DefenseMult = 1f;
        ent.Comp.MeleeDamageCap = 0f;

        ent.Comp.ImmuneToBleed = false;
        ent.Comp.ImmuneToBurn = false;
        ent.Comp.ImmuneToCripple = false;
        ent.Comp.ImmuneToFracture = false;
        ent.Comp.ImmuneToGrapple = false;
        ent.Comp.ImmuneToExplosive = false;
        ent.Comp.ImmuneToSickness = false;
        ent.Comp.UnaffectedByMorale = false;
        ent.Comp.CanAimRanged = true;
        ent.Comp.CanShoveSpecial = false;
        ent.Comp.NoFallDamage = false;

        if (ent.Comp.Perk is not { } perkId)
            return !MathHelper.CloseTo(1f, oldSpeed) || !MathHelper.CloseTo(1f, oldTaken);

        if (!_proto.TryIndex(perkId, out var perk))
        {
            Log.Error($"Unknown Nivalis perk '{perkId}' on {ToPrettyString(ent.Owner)}");
            return !MathHelper.CloseTo(1f, oldSpeed);
        }

        ent.Comp.DamageDealtMult *= perk.DamageDealtMult;
        ent.Comp.DamageTakenMult *= perk.DamageTakenMult;
        ent.Comp.SpeedMult *= perk.SpeedMult;
        ent.Comp.HungerDecayMult *= perk.HungerDecayMult;
        ent.Comp.ThirstDecayMult *= perk.ThirstDecayMult;
        ent.Comp.StaminaDrainMult *= perk.StaminaDrainMult;
        ent.Comp.MoralePenaltyReduction += perk.MoralePenaltyReduction;

        ent.Comp.LightSwingIntervalMult *= perk.LightSwingIntervalMult;
        ent.Comp.HeavySwingIntervalMult *= perk.HeavySwingIntervalMult;
        ent.Comp.MeleeDamageMult *= perk.MeleeDamageMult;
        ent.Comp.FistDamageMult *= perk.FistDamageMult;
        ent.Comp.ShoveCooldownMult *= perk.ShoveCooldownMult;
        ent.Comp.SemiAutoFireRateMult *= perk.SemiAutoFireRateMult;
        ent.Comp.FanningFireRateMult *= perk.FanningFireRateMult;
        ent.Comp.RecoilMult *= perk.RecoilMult;
        ent.Comp.HipFireSpreadMult *= perk.HipFireSpreadMult;
        ent.Comp.ReloadDelayMult *= perk.ReloadDelayMult;
        ent.Comp.AimSpeedMult *= perk.AimSpeedMult;
        ent.Comp.FirearmExplosiveDamageTakenMult *= perk.FirearmExplosiveDamageTakenMult;
        ent.Comp.MaxHealthBonus += perk.MaxHealthBonus;
        ent.Comp.MaxStaminaBonus += perk.MaxStaminaBonus;
        ent.Comp.StaminaRegenMult *= perk.StaminaRegenMult;
        ent.Comp.HealthRegenPerTick += perk.HealthRegenPerTick;
        ent.Comp.HealthRegenInterval += perk.HealthRegenInterval;

        ent.Comp.TrapDeploySpeedMult *= perk.TrapDeploySpeedMult;
        ent.Comp.AttackSpeedMult *= perk.AttackSpeedMult;
        ent.Comp.AmmoScavengeMult *= perk.AmmoScavengeMult;
        ent.Comp.ExplosiveDamageMult *= perk.ExplosiveDamageMult;
        ent.Comp.ShoveSpeedMult *= perk.ShoveSpeedMult;
        ent.Comp.HarvestMult *= perk.HarvestMult;
        ent.Comp.CraftingMult *= perk.CraftingMult;
        ent.Comp.DefenseMult *= perk.DefenseMult;
        ent.Comp.MeleeDamageCap = MathF.Max(ent.Comp.MeleeDamageCap, perk.MeleeDamageCap);

        ent.Comp.ImmuneToBleed = perk.ImmuneToBleed;
        ent.Comp.ImmuneToBurn = perk.ImmuneToBurn;
        ent.Comp.ImmuneToCripple = perk.ImmuneToCripple;
        ent.Comp.ImmuneToFracture = perk.ImmuneToFracture;
        ent.Comp.ImmuneToGrapple = perk.ImmuneToGrapple;
        ent.Comp.ImmuneToExplosive = perk.ImmuneToExplosive;
        ent.Comp.ImmuneToSickness = perk.ImmuneToSickness;
        ent.Comp.UnaffectedByMorale = perk.UnaffectedByMorale;
        ent.Comp.CanAimRanged = perk.CanAimRanged;
        ent.Comp.CanShoveSpecial = perk.ImmuneToCripple && perk.ImmuneToFracture;
        ent.Comp.NoFallDamage = perk.NoFallDamage;

        return !MathHelper.CloseTo(oldSpeed, ent.Comp.SpeedMult)
               || !MathHelper.CloseTo(oldTaken, ent.Comp.DamageTakenMult)
               || !MathHelper.CloseTo(oldMaxHealth, ent.Comp.MaxHealthBonus)
               || !MathHelper.CloseTo(oldMaxStamina, ent.Comp.MaxStaminaBonus);
    }
}
