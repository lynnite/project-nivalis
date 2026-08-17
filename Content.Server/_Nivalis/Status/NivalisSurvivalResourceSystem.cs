using Content.Shared._Nivalis.Perks;
using Content.Shared._Nivalis.Status;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Systems;
using Content.Shared.EntityEffects.Effects.Body;
using Content.Shared.Movement.Systems;
using Content.Shared.Nutrition;
using Content.Shared.Nutrition.EntitySystems;
using Content.Shared.StatusEffectNew;
using Robust.Shared.Prototypes;

namespace Content.Server._Nivalis.Status;

public sealed partial class NivalisSurvivalResourceSystem : EntitySystem
{
    public static readonly EntProtoId HardshipEffect = "StatusEffectNivalisHardship";

    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly MovementModStatusSystem _movementMod = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly StatusEffectsSystem _status = default!;

    private EntityQuery<DamageableComponent> _damageableQuery = default!;

    public override void Initialize()
    {
        base.Initialize();

        _damageableQuery = GetEntityQuery<DamageableComponent>();

        SubscribeLocalEvent<NivalisSurvivalResourceComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<NivalisSurvivalResourceComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<NivalisSurvivalResourceComponent, IngestedEvent>(OnIngested);
    }

    private void OnMapInit(Entity<NivalisSurvivalResourceComponent> ent, ref MapInitEvent args)
    {
        ent.Comp.Hunger = ent.Comp.MaxHunger;
        ent.Comp.Thirst = ent.Comp.MaxThirst;
        RefreshCriticalState(ent);
        Dirty(ent);
    }

    private void OnShutdown(Entity<NivalisSurvivalResourceComponent> ent, ref ComponentShutdown args)
    {
        _status.TryRemoveStatusEffect(ent.Owner, HardshipEffect);
    }

    private void OnIngested(Entity<NivalisSurvivalResourceComponent> ent, ref IngestedEvent args)
    {
        var satiation = CalculateSatiation(args.Split);

        if (satiation.Hunger > 0f || satiation.Thirst > 0f)
        {
            SetHunger(ent, ent.Comp.Hunger + satiation.Hunger);
            SetThirst(ent, ent.Comp.Thirst + satiation.Thirst);
        }
    }

    private (float Hunger, float Thirst) CalculateSatiation(Solution solution)
    {
        float hunger = 0f, thirst = 0f;
        foreach (var quantity in solution.Contents)
        {
            if (!_proto.TryIndex(quantity.Reagent.Prototype, out ReagentPrototype? reagent)
                || reagent.Metabolisms == null)
                continue;

            foreach (var entry in reagent.Metabolisms.Metabolisms.Values)
            {
                foreach (var effect in entry.Effects)
                {
                    if (effect is not Satiate satiate)
                        continue;

                    if (satiate.SatiationType == SatiationSystem.Hunger)
                        hunger += satiate.Factor * quantity.Quantity.Float();
                    else if (satiate.SatiationType == SatiationSystem.Thirst)
                        thirst += satiate.Factor * quantity.Quantity.Float();
                }
            }
        }

        return (hunger, thirst);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<NivalisSurvivalResourceComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            if (Paused(uid))
                continue;

            var changed = false;

            TryComp<NivalisPerkComponent>(uid, out var perks);

            if (comp.Hunger > 0f)
            {
                var hungerDecay = comp.HungerDecay * (perks?.HungerDecayMult ?? 1f);
                comp.Hunger = MathF.Max(0f, comp.Hunger - hungerDecay * frameTime);
                changed = true;
            }

            if (comp.Thirst > 0f)
            {
                var thirstDecay = comp.ThirstDecay * (perks?.ThirstDecayMult ?? 1f);
                comp.Thirst = MathF.Max(0f, comp.Thirst - thirstDecay * frameTime);
                changed = true;
            }

            if (_damageableQuery.TryGetComponent(uid, out var damageable))
            {
                if (comp.Hunger <= 0f)
                    _damageable.ChangeDamage((uid, damageable), comp.StarvationDamage, true, false, origin: uid);

                if (comp.Thirst <= 0f)
                    _damageable.ChangeDamage((uid, damageable), comp.DehydrationDamage, true, false, origin: uid);
            }

            RefreshCriticalState((uid, comp));

            if (changed)
                Dirty(uid, comp);
        }
    }

    private void RefreshCriticalState(Entity<NivalisSurvivalResourceComponent> ent)
    {
        var starving = ent.Comp.Hunger <= ent.Comp.MaxHunger * ent.Comp.HungerCriticalFraction;
        var dehydrated = ent.Comp.Thirst <= ent.Comp.MaxThirst * ent.Comp.ThirstCriticalFraction;
        var inHardship = starving || dehydrated;

        if (inHardship)
        {
            _movementMod.TryAddMovementSpeedModDuration(ent.Owner, HardshipEffect, TimeSpan.FromSeconds(30), 0.8f, 0.8f);
        }
        else
        {
            _status.TryRemoveStatusEffect(ent.Owner, HardshipEffect);
        }
    }

    public void SetHunger(Entity<NivalisSurvivalResourceComponent> ent, float hunger)
    {
        ent.Comp.Hunger = Math.Clamp(hunger, 0f, ent.Comp.MaxHunger);
        RefreshCriticalState(ent);
        Dirty(ent);
    }

    public void SetThirst(Entity<NivalisSurvivalResourceComponent> ent, float thirst)
    {
        ent.Comp.Thirst = Math.Clamp(thirst, 0f, ent.Comp.MaxThirst);
        RefreshCriticalState(ent);
        Dirty(ent);
    }
}

