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

/// <summary>
///     Handles the reworked Nivalis bleeding. Weapons with
///     <see cref="NivalisBleedComponent"/> apply a constant status-effect-based bleed to
///     their victims which deals a flat amount of brute per second without stacking.
/// </summary>
public sealed partial class NivalisBleedSystem : EntitySystem
{
    public static readonly EntProtoId BleedEffect = "StatusEffectNivalisBleed";
    private static readonly TimeSpan BleedDuration = TimeSpan.FromSeconds(40);

    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly StatusEffectsSystem _status = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    private EntityQuery<DamageableComponent> _damageQuery = default!;
    private EntityQuery<MobStateComponent> _mobStateQuery = default!;

    public override void Initialize()
    {
        base.Initialize();

        _damageQuery = GetEntityQuery<DamageableComponent>();
        _mobStateQuery = GetEntityQuery<MobStateComponent>();

        // Broadcast: catches every scavenger melee hit and applies the constant bleed for
        // weapons that carry NivalisBleedComponent. Standard bloodstream bleed suppression is
        // handled separately by NivalisBleedSuppressionSystem.
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

    /// <summary>
    ///     Applies the Nivalis bleed to a target. Does nothing if the target is already bleeding
    ///     (no stacking) or if it can't bleed.
    /// </summary>
    public void TryApplyBleed(EntityUid target, EntityUid source, float damagePerSecond)
    {
        if (!_mobStateQuery.HasComp(target) || !_damageQuery.HasComp(target))
            return;

        // No stacking: only apply the effect if the target isn't already bleeding.
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
            // Cleanup once the underlying status effect has expired or been removed.
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
