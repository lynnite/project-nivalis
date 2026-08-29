using Content.Shared._Nivalis.Stamina;
using Content.Shared.Movement.Components;
using Content.Shared.Movement.Systems;
using Content.Shared.StatusEffectNew;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Server._Nivalis.Stamina;

public sealed partial class NivalisStaminaSystem : EntitySystem
{
    public static readonly EntProtoId ExhaustionEffect = "StatusEffectNivalisExhaustion";

    [Dependency] private MovementModStatusSystem _movementMod = default!;
    [Dependency] private StatusEffectsSystem _status = default!;
    [Dependency] private IGameTiming _timing = default!;
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
        ent.Comp.Exhaustion = 0f;
        UpdateExhaustion(ent);
        Dirty(ent);
    }

    private void OnShutdown(Entity<NivalisStaminaComponent> ent, ref ComponentShutdown args)
    {
        _status.TryRemoveStatusEffect(ent.Owner, ExhaustionEffect);
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

            if (sprinting)
            {
                comp.Exhaustion = MathF.Min(comp.ExhaustionMax, comp.Exhaustion + comp.SprintExhaustionDrain * frameTime);
                comp.LastExertion = _timing.CurTime;
            }
            else
            {
                if (_timing.CurTime - comp.LastExertion >= TimeSpan.FromSeconds(comp.RegenDelay))
                {
                    comp.Current = MathF.Min(comp.Max, comp.Current + comp.RecoveryRate * frameTime);
                    comp.Exhaustion = MathF.Max(0f, comp.Exhaustion - comp.ExhaustionRecoveryRate * frameTime);
                }
            }

            UpdateExhaustion((uid, comp));

            Dirty(uid, comp);
        }
    }

    private void UpdateExhaustion(Entity<NivalisStaminaComponent> ent)
    {
        var shouldBeExhausted = ent.Comp.Exhaustion >= ent.Comp.ExhaustionMax;
        if (ent.Comp.Exhausted == shouldBeExhausted && !shouldBeExhausted)
            return;

        ent.Comp.Exhausted = shouldBeExhausted;

        if (shouldBeExhausted)
        {
            _movementMod.TryAddMovementSpeedModDuration(ent.Owner, ExhaustionEffect, TimeSpan.FromSeconds(30), 0.8f, 0.6f);
        }
        else
        {
            _status.TryRemoveStatusEffect(ent.Owner, ExhaustionEffect);
        }

        Dirty(ent);
    }
}

