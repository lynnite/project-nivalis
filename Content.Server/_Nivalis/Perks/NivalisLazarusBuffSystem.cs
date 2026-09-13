using System.Linq;
using Content.Server._Nivalis.Status;
using Content.Shared._Nivalis.Melee;
using Content.Shared._Nivalis.Perks;
using Content.Shared._Nivalis.Status;
using Content.Shared.StatusEffectNew;
using Content.Shared.Damage;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Systems;
using Content.Shared.FixedPoint;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Movement.Systems;
using Robust.Shared.Timing;

namespace Content.Server._Nivalis.Perks;

public sealed partial class NivalisLazarusBuffSystem : EntitySystem
{
    private const float MeleeBonus = 0.15f;

    private const float DefenseMult = 0.75f;

    private const float DeathPreventionHp = 20f;

    [Dependency] private DamageableSystem _damageable = default!;
    [Dependency] private MobThresholdSystem _mobThreshold = default!;
    [Dependency] private StatusEffectsSystem _status = default!;
    [Dependency] private NivalisBleedSystem _bleed = default!;
    [Dependency] private MovementSpeedModifierSystem _moveSpeed = default!;
    [Dependency] private IGameTiming _timing = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<NivalisLazarusBuffComponent, DamageModifyEvent>(OnDamageModify);
        SubscribeLocalEvent<NivalisLazarusBuffComponent, RefreshMovementSpeedModifiersEvent>(OnRefreshSpeed);
        SubscribeLocalEvent<NivalisMeleeHitEvent>(OnMeleeHit);
    }

    private void OnDamageModify(Entity<NivalisLazarusBuffComponent> ent, ref DamageModifyEvent args)
    {
        if (!ent.Comp.Beneficial)
            return;

        args.Damage *= DefenseMult;

        if (args.Damage.DamageDict.TryGetValue("Bloodloss", out var bloodloss) && bloodloss > FixedPoint2.Zero)
        {
            args.Damage.DamageDict["Bloodloss"] = FixedPoint2.Zero;
        }

        ClampToDeathFloor(ent.Owner, args.Damage);
    }

    private void ClampToDeathFloor(EntityUid uid, DamageSpecifier damage)
    {
        if (!TryComp<DamageableComponent>(uid, out var damageable))
            return;

        if (!TryComp<MobThresholdsComponent>(uid, out var thresholds))
            return;

        var maxHealth = _mobThreshold.GetThresholdForState(uid, MobState.Dead, thresholds).Float();
        if (maxHealth <= 0f)
            return;

        var currentHp = maxHealth - _damageable.GetTotalDamage((uid, damageable)).Float();
        var allowed = MathF.Max(0f, currentHp - DeathPreventionHp);

        var positive = DamageSpecifier.GetPositive(damage);
        var total = positive.GetTotal().Float();
        if (total <= allowed || total <= 0f)
            return;

        var scale = allowed / total;
        ScalePositiveDamage(damage, scale);
    }

    private static void ScalePositiveDamage(DamageSpecifier damage, float scale)
    {
        foreach (var (type, value) in damage.DamageDict.ToArray())
        {
            if (value <= FixedPoint2.Zero)
                continue;

            damage.DamageDict[type] = value * scale;
        }
    }

    private void OnMeleeHit(NivalisMeleeHitEvent args)
    {
        if (!TryComp<NivalisLazarusBuffComponent>(args.User, out var buff) || !buff.Beneficial)
            return;

        if (!args.IsHit)
            return;

        args.BonusDamage += args.Damage * MeleeBonus;
    }

    private void OnRefreshSpeed(Entity<NivalisLazarusBuffComponent> ent, ref RefreshMovementSpeedModifiersEvent args)
    {
        if (!ent.Comp.Beneficial)
            return;

        if (!TryComp<NivalisFractureComponent>(ent.Owner, out var fracture))
            return;

        if (fracture.LegFractured)
        {
            const float compensation = 0.775f / 0.55f;
            args.ModifySpeed(compensation, compensation);
        }
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<NivalisLazarusBuffComponent>();
        while (query.MoveNext(out var uid, out var buff))
        {
            if (Deleted(uid))
                continue;

            if (_timing.CurTime < buff.ExpiresAt)
            {
                if (buff.Beneficial && _status.HasStatusEffect(uid, NivalisBleedSystem.BleedEffect))
                {
                    _bleed.CleanseBleedStatusOnly(uid);
                }

                continue;
            }

            RemComp<NivalisLazarusBuffComponent>(uid);
            _moveSpeed.RefreshMovementSpeedModifiers(uid);
        }
    }
}
