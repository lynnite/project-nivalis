using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Numerics;
using Content.Shared._Nivalis.Perks;
using Content.Shared.Damage;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Systems;
using Content.Shared.Interaction;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Movement.Systems;
using Content.Shared.Physics;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Map;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server._Nivalis.Perks;

public sealed partial class NivalisVagabondSystem : EntitySystem
{
    public const string VagabondPerk = "Vagabond";

    private const float SwingRange = 2.4f;
    private static readonly Angle SwingArc = Angle.FromDegrees(90);
    private const int AttackMask = (int)(CollisionGroup.MobMask | CollisionGroup.Opaque);
    private const int MaxTargets = 5;

    private const float SpeedPerTag = 0.10f;

    private static readonly EntProtoId SlashEffect = "NivalisKiraSlash";
    private static readonly SoundPathSpecifier SwingSound = new("/Audio/Weapons/bladeslice.ogg");

    [Dependency] private DamageableSystem _damageable = default!;
    [Dependency] private MobStateSystem _mobState = default!;
    [Dependency] private MobThresholdSystem _mobThreshold = default!;
    [Dependency] private SharedTransformSystem _transform = default!;
    [Dependency] private MovementSpeedModifierSystem _moveSpeed = default!;
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private SharedInteractionSystem _interaction = default!;
    [Dependency] private SharedPhysicsSystem _physics = default!;
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private IRobustRandom _random = default!;

    private readonly Dictionary<EntityUid, (float Amount, TimeSpan Since)> _burstDamage = new();

    public override void Initialize()
    {
        base.Initialize();

        SubscribeNetworkEvent<NivalisPerkAbilityPressedMessage>(OnAbilityPressed);
        SubscribeLocalEvent<NivalisVagabondComponent, DamageChangedEvent>(OnDamageTaken);
        SubscribeLocalEvent<NivalisVagabondComponent, RefreshMovementSpeedModifiersEvent>(OnRefreshSpeed);
        SubscribeLocalEvent<NivalisVagabondComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<MobStateChangedEvent>(OnMobStateChanged);
    }

    private bool TryGetVagabond(EntityUid uid, [NotNullWhen(true)] out NivalisVagabondComponent? vag)
    {
        vag = null;
        if (!TryComp<NivalisPerkComponent>(uid, out var perk) || perk.Perk?.Id != VagabondPerk)
            return false;

        vag = EnsureComp<NivalisVagabondComponent>(uid);
        return true;
    }

    private void OnAbilityPressed(NivalisPerkAbilityPressedMessage msg, EntitySessionEventArgs args)
    {
        if (args.SenderSession.AttachedEntity is not { } uid)
            return;

        if (!TryGetVagabond(uid, out var vag))
            return;

        if (msg.Holding)
            RedeemTags(uid, vag);
        else
            SwingKira(uid, vag, msg.AimDirection);
    }

    private void SwingKira(EntityUid user, NivalisVagabondComponent vag, Vector2 aim)
    {
        if (_mobState.IsDead(user))
            return;

        var now = _timing.CurTime;
        if (now < vag.NextSwingTime)
            return;

        var fullMeter = vag.DogTags >= vag.FullMeterTagThreshold;

        if (aim.LengthSquared() < 0.0001f)
            aim = _transform.GetWorldRotation(user).ToWorldVec();
        else
            aim = Vector2.Normalize(aim);

        var damage = MathHelper.Lerp(vag.MinSwingDamage, vag.MaxSwingDamage, Math.Clamp(vag.AbilityPercent, 0f, 100f) / 100f);

        vag.NextSwingTime = now + vag.SwingCooldown;
        vag.AbilityPercent = fullMeter ? 100f : 0f;
        Dirty(user, vag);

        var origin = _transform.GetWorldPosition(user);
        var mapId = Transform(user).MapID;

        var slash = Spawn(SlashEffect, Transform(user).Coordinates);
        _transform.SetWorldRotation(slash, aim.ToWorldAngle() + Angle.FromDegrees(180));

        _audio.PlayPvs(SwingSound, user);

        var targets = ArcRayCast(origin, aim.ToWorldAngle(), SwingArc, SwingRange, mapId, user)
            .Take(MaxTargets)
            .ToList();

        if (targets.Count == 0)
            return;

        foreach (var target in targets)
        {
            if (target == user || _mobState.IsDead(target))
                continue;

            _damageable.TryChangeDamage(target, new DamageSpecifier
            {
                DamageDict = { ["Slash"] = damage },
            }, origin: user, ignoreResistances: false);
        }
    }

    private void RedeemTags(EntityUid user, NivalisVagabondComponent vag)
    {
        if (vag.DogTags <= 0)
            return;

        if (!TryComp<DamageableComponent>(user, out var damageable))
            return;

        var healingNeeded = GetMissingHealth(user, damageable);
        if (healingNeeded <= 0f)
            return;

        var perTag = vag.HealPerTag;
        var tagsNeeded = (int)MathF.Ceiling(healingNeeded / perTag);
        var tagsSpent = Math.Min(tagsNeeded, vag.DogTags);

        if (tagsSpent <= 0)
            return;

        vag.DogTags -= tagsSpent;
        Dirty(user, vag);

        _damageable.TryChangeDamage(user, new DamageSpecifier
        {
            DamageDict = { ["Brute"] = -(tagsSpent * perTag) },
        }, ignoreResistances: true);

        _moveSpeed.RefreshMovementSpeedModifiers(user);
    }

    private float GetMissingHealth(EntityUid user, DamageableComponent damageable)
    {
        if (!TryComp<MobThresholdsComponent>(user, out var thresholds))
            return 0f;

        var maxHealth = _mobThreshold.GetThresholdForState(user, MobState.Dead, thresholds);
        var currentDamage = _damageable.GetTotalDamage((user, damageable));
        return MathF.Max(0f, maxHealth.Float() - currentDamage.Float());
    }

    private void OnDamageTaken(Entity<NivalisVagabondComponent> ent, ref DamageChangedEvent args)
    {
        var vag = ent.Comp;
        if (vag.DogTags <= 0)
            return;

        if (!args.DamageIncreased || args.DamageDelta == null)
            return;

        var amount = args.DamageDelta.GetTotal().Float();
        if (amount <= 0f)
            return;

        var now = _timing.CurTime;

        if (_random.Prob(vag.ChipDamageTagChance))
        {
            vag.DogTags = Math.Max(0, vag.DogTags - 1);
        }

        if (!_burstDamage.TryGetValue(ent.Owner, out var burst) || now - burst.Since > vag.BurstWindow)
            burst = (0f, now);

        burst = (burst.Amount + amount, burst.Since);
        _burstDamage[ent.Owner] = burst;

        if (burst.Amount >= vag.BurstDamageThreshold)
        {
            vag.DogTags /= 2;
            _burstDamage[ent.Owner] = (0f, now);
        }

        Dirty(ent.Owner, vag);
        _moveSpeed.RefreshMovementSpeedModifiers(ent.Owner);
    }

    private void OnRefreshSpeed(Entity<NivalisVagabondComponent> ent, ref RefreshMovementSpeedModifiersEvent args)
    {
        var bonus = Math.Min(ent.Comp.DogTags, ent.Comp.MaxDogTags) * SpeedPerTag;
        args.ModifySpeed(1f + bonus, 1f + bonus);
    }

    private void OnShutdown(Entity<NivalisVagabondComponent> ent, ref ComponentShutdown args)
    {
        _burstDamage.Remove(ent.Owner);
    }

    private void OnMobStateChanged(MobStateChangedEvent ev)
    {
        if (ev.NewMobState != MobState.Dead || ev.Origin is not { } killer)
            return;

        if (ev.Target == killer)
            return;

        if (!TryGetVagabond(killer, out var vag))
            return;

        if (vag.DogTags >= vag.MaxDogTags)
            return;

        vag.DogTags = Math.Min(vag.MaxDogTags, vag.DogTags + 1);
        Dirty(killer, vag);
        _moveSpeed.RefreshMovementSpeedModifiers(killer);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<NivalisVagabondComponent>();
        while (query.MoveNext(out var uid, out var vag))
        {
            if (Deleted(uid) || Terminating(uid))
                continue;

            var target = vag.DogTags >= vag.FullMeterTagThreshold
                ? 100f
                : Math.Clamp(vag.AbilityPercent + vag.AbilityRechargeRate * frameTime, 0f, 100f);

            if (MathHelper.CloseTo(vag.AbilityPercent, target))
                continue;

            vag.AbilityPercent = target;
            Dirty(uid, vag);
        }
    }

    private HashSet<EntityUid> ArcRayCast(Vector2 position, Angle angle, Angle arcWidth, float range, MapId mapId, EntityUid ignore)
    {
        var increments = 1 + 35 * (int)Math.Ceiling(arcWidth / (2 * Math.PI));
        var increment = arcWidth / increments;
        var baseAngle = angle - arcWidth / 2;

        var resSet = new HashSet<EntityUid>();

        for (var i = 0; i < increments; i++)
        {
            var castAngle = new Angle(baseAngle + increment * i);
            var res = _physics.IntersectRay(mapId,
                new CollisionRay(position, castAngle.ToWorldVec(), AttackMask),
                range,
                ignore,
                false)
                .ToList();

            if (res.Count == 0)
                continue;

            var resChecked = res.Where(x => x.Distance.Equals(res[0].Distance));
            foreach (var r in resChecked)
            {
                if (_interaction.InRangeUnobstructed(ignore, r.HitEntity, range + 0.1f, overlapCheck: false))
                    resSet.Add(r.HitEntity);
            }
        }

        return resSet;
    }
}
