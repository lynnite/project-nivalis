using Content.Shared._Nivalis.Melee;
using Content.Shared._Nivalis.Status;
using Content.Shared.Damage;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Prototypes;
using Content.Shared.Damage.Systems;
using Content.Shared.FixedPoint;
using Content.Shared.Mobs.Components;
using Content.Shared.StatusEffectNew;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Server._Nivalis.Status;

public sealed partial class NivalisBleedSystem : EntitySystem
{
    public static readonly EntProtoId BleedEffect = "StatusEffectNivalisBleed";
    private static readonly TimeSpan BleedDuration = TimeSpan.FromSeconds(40);

    [Dependency] private DamageableSystem _damageable = default!;
    [Dependency] private StatusEffectsSystem _status = default!;
    [Dependency] private IGameTiming _timing = default!;

    private EntityQuery<DamageableComponent> _damageQuery = default!;
    private EntityQuery<MobStateComponent> _mobStateQuery = default!;

    public override void Initialize()
    {
        base.Initialize();

        _damageQuery = GetEntityQuery<DamageableComponent>();
        _mobStateQuery = GetEntityQuery<MobStateComponent>();

        SubscribeLocalEvent<NivalisMeleeHitEvent>(OnMeleeHit);
    }

    private void OnMeleeHit(NivalisMeleeHitEvent args)
    {
        if (!TryComp<NivalisBleedComponent>(args.Weapon, out var bleedComp))
            return;

        foreach (var target in args.HitEntities)
        {
            if (!_mobStateQuery.HasComp(target) || !_damageQuery.HasComp(target))
                continue;

            TryApplyBleed(target, args.User, bleedComp.DamagePerSecond);
        }
    }

    public void TryApplyBleed(EntityUid target, EntityUid source, float damagePerSecond)
    {
        if (!_mobStateQuery.HasComp(target) || !_damageQuery.HasComp(target))
            return;

        if (_status.HasStatusEffect(target, BleedEffect))
            return;

        _status.TryAddStatusEffectDuration(target, BleedEffect, BleedDuration);

        var active = EnsureComp<NivalisBleedActiveComponent>(target);
        active.DamagePerSecond = damagePerSecond;
        active.Source = source;
        active.NextTick = _timing.CurTime + TimeSpan.FromSeconds(1);
        Dirty(target, active);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<NivalisBleedActiveComponent>();
        while (query.MoveNext(out var uid, out var bleed))
        {
            if (!_status.HasStatusEffect(uid, BleedEffect))
            {
                RemComp<NivalisBleedActiveComponent>(uid);
                continue;
            }

            if (!_damageQuery.TryGetComponent(uid, out var damageable))
                continue;

            if (_timing.CurTime < bleed.NextTick)
                continue;

            bleed.NextTick = _timing.CurTime + TimeSpan.FromSeconds(1);
            Dirty(uid, bleed);

            var damage = new DamageSpecifier();
            damage.DamageDict["Brute"] = FixedPoint2.New(bleed.DamagePerSecond);
            _damageable.ChangeDamage((uid, damageable), damage, true, origin: bleed.Source);
        }
    }
}
