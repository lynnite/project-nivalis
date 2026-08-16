using Content.Shared._Nivalis.Perks;
using Content.Shared._Nivalis.Stamina;
using Content.Shared.Movement.Components;
using Content.Shared.Movement.Systems;
using Content.Shared.StatusEffectNew;
using Robust.Shared.Prototypes;

namespace Content.Server._Nivalis.Stamina;

/// <summary>
///     Handles exhaustion and sprint
///     Sprinting drains <see cref="NivalisStaminaComponent"/>. When the pool falls below the
///     exhaustion threshold the mob is slowed until it recovers above the threshold
/// </summary>
public sealed partial class NivalisStaminaSystem : EntitySystem
{
    public static readonly EntProtoId ExhaustionEffect = "StatusEffectNivalisExhaustion";

    [Dependency] private readonly MovementModStatusSystem _movementMod = default!;
    [Dependency] private readonly StatusEffectsSystem _status = default!;
    private EntityQuery<InputMoverComponent> _inputQuery = default!;

    public override void Initialize()
    {
        base.Initialize();

        _inputQuery = GetEntityQuery<InputMoverComponent>();

        SubscribeLocalEvent<NivalisStaminaComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<NivalisStaminaComponent, ComponentShutdown>(OnShutdown);
    }

    private void OnMapInit(Entity<NivalisStaminaComponent> ent, ref MapInitEvent args)
    {
        ent.Comp.Current = ent.Comp.Max;
        UpdateExhaustion(ent);
        Dirty(ent);
    }

    private void OnShutdown(Entity<NivalisStaminaComponent> ent, ref ComponentShutdown args)
    {
        if (_status.TryRemoveStatusEffect(ent.Owner, ExhaustionEffect))
        {
        }
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<NivalisStaminaComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            if (Paused(uid))
                continue;

            var sprinting = _inputQuery.TryGetComponent(uid, out var input) && input.Sprinting;

            if (sprinting && !comp.Exhausted)
            {
                TryComp<NivalisPerkComponent>(uid, out var perks);
                var drain = comp.SprintDrain * (perks?.StaminaDrainMult ?? 1f);
                comp.Current = MathF.Max(comp.ExhaustionThreshold * comp.Max, comp.Current - drain * frameTime);
                if (comp.Current <= comp.ExhaustionThreshold * comp.Max)
                    UpdateExhaustion((uid, comp), exhausted: true);
            }
            else
            {
                comp.Current = MathF.Min(comp.Max, comp.Current + comp.RecoveryRate * frameTime);
                if (comp.Exhausted && comp.Current >= comp.ExhaustionThreshold * comp.Max)
                    UpdateExhaustion((uid, comp), exhausted: false);
            }

            Dirty(uid, comp);
        }
    }

    private void UpdateExhaustion(Entity<NivalisStaminaComponent> ent, bool? exhausted = null)
    {
        var shouldBeExhausted = exhausted ?? ent.Comp.Current <= ent.Comp.ExhaustionThreshold * ent.Comp.Max;
        if (ent.Comp.Exhausted == shouldBeExhausted)
            return;

        ent.Comp.Exhausted = shouldBeExhausted;

        if (shouldBeExhausted)
        {
            _movementMod.TryAddMovementSpeedModDuration(ent.Owner, ExhaustionEffect, TimeSpan.FromSeconds(30), 0.6f, 0.8f);
        }
        else
        {
            _status.TryRemoveStatusEffect(ent.Owner, ExhaustionEffect);
        }

        Dirty(ent);
    }
}

