using Content.Shared._Nivalis.Perks;
using Content.Shared._Nivalis.Status;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Systems;
using Content.Shared.Movement.Systems;
using Content.Shared.StatusEffectNew;
using Robust.Shared.Prototypes;

namespace Content.Server._Nivalis.Status;

public sealed partial class NivalisSurvivalResourceSystem : EntitySystem
{
    public static readonly EntProtoId HardshipEffect = "StatusEffectNivalisHardship";

    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly MovementModStatusSystem _movementMod = default!;
    [Dependency] private readonly StatusEffectsSystem _status = default!;

    private EntityQuery<DamageableComponent> _damageableQuery = default!;

    public override void Initialize()
    {
        base.Initialize();

        _damageableQuery = GetEntityQuery<DamageableComponent>();

        SubscribeLocalEvent<NivalisSurvivalResourceComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<NivalisSurvivalResourceComponent, ComponentShutdown>(OnShutdown);
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

