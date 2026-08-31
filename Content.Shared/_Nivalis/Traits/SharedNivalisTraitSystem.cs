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

        ent.Comp.DamageDealtMult = 1f;
        ent.Comp.DamageTakenMult = 1f;
        ent.Comp.SpeedMult = 1f;
        ent.Comp.HungerDecayMult = 1f;
        ent.Comp.ThirstDecayMult = 1f;
        ent.Comp.StaminaDrainMult = 1f;
        ent.Comp.MoralePenaltyReduction = 0f;

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
        }

        ent.Comp.DamageDealtMult = MathF.Max(0.1f, ent.Comp.DamageDealtMult);
        ent.Comp.DamageTakenMult = MathF.Max(0.1f, ent.Comp.DamageTakenMult);
        ent.Comp.SpeedMult = MathF.Max(0.5f, ent.Comp.SpeedMult);
        ent.Comp.HungerDecayMult = MathF.Max(0.1f, ent.Comp.HungerDecayMult);
        ent.Comp.ThirstDecayMult = MathF.Max(0.1f, ent.Comp.ThirstDecayMult);
        ent.Comp.StaminaDrainMult = MathF.Max(0.1f, ent.Comp.StaminaDrainMult);
        ent.Comp.MoralePenaltyReduction = Math.Clamp(ent.Comp.MoralePenaltyReduction, 0f, 1f);

        var changed = !MathHelper.CloseTo(oldDealt, ent.Comp.DamageDealtMult)
                      || !MathHelper.CloseTo(oldTaken, ent.Comp.DamageTakenMult)
                      || !MathHelper.CloseTo(oldSpeed, ent.Comp.SpeedMult)
                      || !MathHelper.CloseTo(oldHunger, ent.Comp.HungerDecayMult)
                      || !MathHelper.CloseTo(oldThirst, ent.Comp.ThirstDecayMult)
                      || !MathHelper.CloseTo(oldStamina, ent.Comp.StaminaDrainMult)
                      || !MathHelper.CloseTo(oldMorale, ent.Comp.MoralePenaltyReduction);

        return changed;
    }
}
