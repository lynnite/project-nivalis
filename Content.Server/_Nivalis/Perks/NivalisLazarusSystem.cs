using System.Numerics;
using Content.Shared._Nivalis.Combat;
using Content.Shared._Nivalis.Perks;
using Content.Shared._Nivalis.Status;
using Content.Shared.Damage;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Systems;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Movement.Systems;
using Content.Shared.StatusEffectNew;
using Content.Shared.Stunnable;
using Robust.Shared.Map;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Server._Nivalis.Perks;

public sealed partial class NivalisLazarusSystem : EntitySystem
{
    public const string LazarusPerk = "Lazarus";

    public const float Range = 7f;

    private const float CollisionRadius = 0.45f;

    public const float BlastRadius = 5f;

    private static readonly TimeSpan EnemyStun = TimeSpan.FromSeconds(2.5);

    private static readonly TimeSpan EnemySlowDuration = TimeSpan.FromSeconds(8);

    private const float EnemySlowMultiplier = 0.5f;

    public static readonly TimeSpan BuffDuration = TimeSpan.FromSeconds(60);

    public const float HealAmount = 20f;

    private const float ProjectileSpeed = 26f;

    private static readonly EntProtoId ShotPrototype = "NivalisLazarusShot";

    private static readonly EntProtoId SlowEffect = "StatusEffectNivalisLazarusSlowed";

    [Dependency] private SharedTransformSystem _transform = default!;
    [Dependency] private MobStateSystem _mobState = default!;
    [Dependency] private DamageableSystem _damageable = default!;
    [Dependency] private StatusEffectsSystem _status = default!;
    [Dependency] private MovementModStatusSystem _movementMod = default!;
    [Dependency] private IGameTiming _timing = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeNetworkEvent<NivalisPerkAbilityPressedMessage>(OnAbilityPressed);
    }

    private bool TryGetLazarus(EntityUid uid, out NivalisLazarusComponent laz)
    {
        laz = default!;
        if (!TryComp<NivalisPerkComponent>(uid, out var perk) || perk.Perk?.Id != LazarusPerk)
            return false;

        laz = EnsureComp<NivalisLazarusComponent>(uid);
        return true;
    }

    private void OnAbilityPressed(NivalisPerkAbilityPressedMessage msg, EntitySessionEventArgs args)
    {
        if (args.SenderSession.AttachedEntity is not { } uid)
            return;

        if (!TryGetLazarus(uid, out var laz))
            return;

        if (!laz.Initialised)
        {
            laz.Charge = 100f;
            laz.Shots = laz.MaxShots;
            laz.Initialised = true;
        }

        if (_mobState.IsDead(uid))
            return;

        if (_timing.CurTime < laz.NextShotTime)
            return;

        if (laz.Shots <= 0 || laz.Charge < laz.ShotCost - 0.0001f)
            return;

        laz.NextShotTime = _timing.CurTime + laz.ShotCooldown;
        laz.Shots = Math.Max(0, laz.Shots - 1);
        laz.Charge = MathF.Max(0f, laz.Charge - laz.ShotCost);
        Dirty(uid, laz);

        FireShot(uid, msg.Holding, msg.AimDirection);
    }

    private void FireShot(EntityUid user, bool heal, Vector2 aimDirection)
    {
        var aim = aimDirection.LengthSquared() < 0.0001f
            ? _transform.GetWorldRotation(user).ToWorldVec()
            : Vector2.Normalize(aimDirection);

        var mapCoords = _transform.GetMapCoordinates(user);
        var origin = mapCoords.Position;
        var mapId = mapCoords.MapId;

        var target = origin + aim * Range;

        var shot = Spawn(ShotPrototype, _transform.ToCoordinates(new MapCoordinates(origin + aim * 0.4f, mapId)));
        var shotComp = EnsureComp<NivalisLazarusShotComponent>(shot);
        shotComp.Shooter = user;
        shotComp.Target = target;
        shotComp.Heal = heal;

        _transform.SetWorldRotation(shot, MathHelper.PiOver2);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var perkQuery = EntityQueryEnumerator<NivalisLazarusComponent>();
        while (perkQuery.MoveNext(out var uid, out var laz))
        {
            if (Deleted(uid))
                continue;

            if (laz.Charge >= 100f)
            {
                if (laz.Charge != 100f || laz.Shots != laz.MaxShots)
                {
                    laz.Charge = 100f;
                    laz.Shots = laz.MaxShots;
                    Dirty(uid, laz);
                }
                continue;
            }

            var oldPercent = (int) laz.Charge;
            var oldShots = laz.Shots;

            laz.Charge = Math.Clamp(laz.Charge + laz.RechargeRate * frameTime, 0f, 100f);
            laz.Shots = Math.Min(laz.MaxShots, (int) (laz.Charge / laz.ShotCost));

            if ((int) laz.Charge != oldPercent || laz.Shots != oldShots)
                Dirty(uid, laz);
        }

        var shotQuery = EntityQueryEnumerator<NivalisLazarusShotComponent, TransformComponent>();
        while (shotQuery.MoveNext(out var shotUid, out var shot, out var xform))
        {
            if (shot.Detonated)
                continue;

            var pos = _transform.GetWorldPosition(xform);
            var mapId = xform.MapID;
            var toTarget = shot.Target - pos;
            var distance = toTarget.Length();

            var step = ProjectileSpeed * frameTime;

            var dir = distance > 0.001f ? toTarget / distance : Vector2.Zero;
            var hit = FindCollision(pos, dir, step, distance, mapId, shot.Shooter);
            if (hit is { } hitUid)
            {
                var hitPos = _transform.GetWorldPosition(hitUid);
                Detonate(shotUid, shot, hitPos);
                continue;
            }

            if (step >= distance)
            {
                Detonate(shotUid, shot, shot.Target);
                continue;
            }

            _transform.SetWorldPosition(shotUid, pos + dir * step);
        }
    }

    private EntityUid? FindCollision(Vector2 pos, Vector2 dir, float step, float remaining, MapId mapId, EntityUid shooter)
    {
        var travel = MathF.Min(step, remaining);
        if (travel <= 0f || dir == Vector2.Zero)
            return null;

        var query = EntityQueryEnumerator<DamageableComponent, MobStateComponent, TransformComponent>();
        EntityUid? best = null;
        var bestAlong = float.MaxValue;

        while (query.MoveNext(out var ent, out _, out _, out var xform))
        {
            if (ent == shooter || _mobState.IsDead(ent))
                continue;

            if (xform.MapID != mapId)
                continue;

            var entPos = _transform.GetWorldPosition(xform);
            var toEnt = entPos - pos;

            var along = Vector2.Dot(toEnt, dir);
            if (along < 0.001f || along > travel + CollisionRadius)
                continue;

            var lateral = (toEnt - dir * along).Length();
            if (lateral > CollisionRadius)
                continue;

            if (along < bestAlong)
            {
                bestAlong = along;
                best = ent;
            }
        }

        return best;
    }

    private void Detonate(EntityUid shotUid, NivalisLazarusShotComponent shot, Vector2 epicenter)
    {
        shot.Detonated = true;

        var mapId = _transform.GetMapCoordinates(shotUid).MapId;
        var shooter = shot.Shooter;

        QueueDel(shotUid);

        var query = EntityQueryEnumerator<DamageableComponent, MobStateComponent, TransformComponent>();
        while (query.MoveNext(out var target, out var damageable, out var mobState, out var xform))
        {
            if (_mobState.IsDead(target))
                continue;

            if (xform.MapID != mapId)
                continue;

            var targetPos = _transform.GetWorldPosition(xform);
            var dist = (targetPos - epicenter).Length();
            if (dist > BlastRadius)
                continue;

            var isAlly = target == shooter || IsAlly(shooter, target);

            if (isAlly)
            {
                if (shot.Heal)
                    HealAlly(target);
                else
                    ApplyBuff(target, shooter);
            }
            else
            {
                DebuffEnemy(target);
            }
        }

        if (!_mobState.IsDead(shooter))
        {
            if (shot.Heal)
                HealAlly(shooter);
            else
                ApplyBuff(shooter, shooter);
        }
    }

    private bool IsAlly(EntityUid shooter, EntityUid target)
    {
        if (!TryComp<NivalisFriendlyFireComponent>(shooter, out var sf) || sf.Team == NivalisCombatTeam.None)
            return false;

        if (!TryComp<NivalisFriendlyFireComponent>(target, out var tf) || tf.Team == NivalisCombatTeam.None)
            return false;

        return sf.Team == tf.Team;
    }

    private void HealAlly(EntityUid target)
    {
        _damageable.TryChangeDamage(target, new DamageSpecifier
        {
            DamageDict = { ["Brute"] = -HealAmount },
        }, ignoreResistances: true);
    }

    private void ApplyBuff(EntityUid target, EntityUid shooter)
    {
        var buff = EnsureComp<NivalisLazarusBuffComponent>(target);
        buff.Beneficial = true;
        buff.Source = shooter;
        buff.ExpiresAt = _timing.CurTime + BuffDuration;
        Dirty(target, buff);

        if (TryComp<ActorComponent>(target, out _))
        {
            var filter = Filter.Entities(target);
            RaiseNetworkEvent(new NivalisLazarusBuffMessage((float) BuffDuration.TotalSeconds), filter);
        }
    }

    private void DebuffEnemy(EntityUid target)
    {
        _status.TryAddStatusEffectDuration(target, SharedStunSystem.StunId, EnemyStun);
        _movementMod.TryAddMovementSpeedModDuration(target, SlowEffect, EnemySlowDuration, EnemySlowMultiplier);
    }
}
