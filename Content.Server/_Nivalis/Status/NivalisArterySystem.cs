using Content.Shared._Nivalis.Status;
using Content.Shared.Damage;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Prototypes;
using Content.Shared.Damage.Systems;
using Content.Shared.FixedPoint;
using Content.Shared.Mobs.Components;
using Content.Shared.Projectiles;
using Content.Shared.StatusEffectNew;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server._Nivalis.Status;

public sealed partial class NivalisArterySystem : EntitySystem
{
    public static readonly EntProtoId ArteryEffect = "StatusEffectNivalisArtery";
    public static readonly EntProtoId ImmunityEffect = "StatusEffectNivalisBleedImmunity";

    private const float ArteryChance = 0.05f;
    private const float ArteryBrutePerSecond = 1.0f;

    [Dependency] private DamageableSystem _damageable = default!;
    [Dependency] private StatusEffectsSystem _status = default!;
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private IRobustRandom _random = default!;

    private EntityQuery<DamageableComponent> _damageQuery = default!;
    private EntityQuery<MobStateComponent> _mobStateQuery = default!;

    public override void Initialize()
    {
        base.Initialize();

        _damageQuery = GetEntityQuery<DamageableComponent>();
        _mobStateQuery = GetEntityQuery<MobStateComponent>();

        SubscribeLocalEvent<ProjectileHitEvent>(OnProjectileHit);
    }

    private void OnProjectileHit(ref ProjectileHitEvent args)
    {
        var target = args.Target;
        if (!_mobStateQuery.HasComp(target) || !_damageQuery.HasComp(target))
            return;

        if (!HasComp<NivalisSurvivalResourceComponent>(target))
            return;

        if (!_random.Prob(ArteryChance))
            return;

        TryApplyArtery(target, args.Shooter);
    }

    public void TryApplyArtery(EntityUid target, EntityUid? source)
    {
        if (!_mobStateQuery.HasComp(target) || !_damageQuery.HasComp(target))
            return;

        _status.TrySetStatusEffectDuration(target, ArteryEffect, out _);

        var active = EnsureComp<NivalisArteryActiveComponent>(target);
        active.Source = source;
        active.NextTick = _timing.CurTime + TimeSpan.FromSeconds(1);
        Dirty(target, active);
    }

    public bool HasArtery(EntityUid uid)
    {
        return _status.HasStatusEffect(uid, ArteryEffect);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<NivalisArteryActiveComponent>();
        while (query.MoveNext(out var uid, out var artery))
        {
            if (!_status.HasStatusEffect(uid, ArteryEffect))
            {
                RemComp<NivalisArteryActiveComponent>(uid);
                continue;
            }

            if (!_damageQuery.TryGetComponent(uid, out var damageable))
                continue;

            if (_timing.CurTime < artery.NextTick)
                continue;

            artery.NextTick = _timing.CurTime + TimeSpan.FromSeconds(1);
            Dirty(uid, artery);

            if (_status.HasStatusEffect(uid, ImmunityEffect))
                continue;

            var damage = new DamageSpecifier();
            damage.DamageDict["Brute"] = FixedPoint2.New(ArteryBrutePerSecond);
            _damageable.ChangeDamage((uid, damageable), damage, true, origin: artery.Source);
        }
    }
}
