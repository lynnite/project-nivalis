using System.Numerics;
using Content.Shared._Nivalis.Perks;
using Content.Shared.Damage;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Systems;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.StatusEffectNew;
using Content.Shared.Stunnable;
using Content.Shared.Throwing;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Spawners;
using Robust.Shared.Timing;
using TimedDespawnComponent = Robust.Shared.Spawners.TimedDespawnComponent;

namespace Content.Server._Nivalis.Perks;

public sealed partial class NivalisBlitzerSystem : EntitySystem
{
    public const string BlitzerPerk = "Blitzer";

    private const float ThrowDistance = 1.6f;

    private const float BurstRadius = 4.0f;

    private const float SparkScatter = 3.5f;

    private static readonly TimeSpan EdgeCooldown = TimeSpan.FromSeconds(0.35);

    private static readonly EntProtoId BombPrototype = "NivalisBlitzerBomb";

    private static readonly EntProtoId SparkEffect = "EffectSparks";

    private static readonly EntProtoId StreakSpark = "NivalisGlowStreak";

    private static readonly Angle AngleOffset90 = Angle.FromDegrees(90);

    [Dependency] private DamageableSystem _damageable = default!;
    [Dependency] private SharedTransformSystem _transform = default!;
    [Dependency] private MobStateSystem _mobState = default!;
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private StatusEffectsSystem _status = default!;
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private ThrowingSystem _throw = default!;
    [Dependency] private IRobustRandom _random = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeNetworkEvent<NivalisPerkAbilityPressedMessage>(OnAbilityPressed);
    }

    private void OnAbilityPressed(NivalisPerkAbilityPressedMessage msg, EntitySessionEventArgs args)
    {
        if (args.SenderSession.AttachedEntity is not { } uid)
            return;

        if (!TryComp<NivalisPerkComponent>(uid, out var perk) || perk.Perk?.Id != BlitzerPerk)
            return;

        var blitz = EnsureComp<NivalisBlitzerComponent>(uid);
        if (!blitz.Initialised)
        {
            blitz.Charge = 100f;
            blitz.Initialised = true;
        }

        if (_timing.CurTime < blitz.NextActionAt)
            return;
        blitz.NextActionAt = _timing.CurTime + EdgeCooldown;

        if (msg.Holding)
        {
            TryDetonateAll(uid, blitz);
            return;
        }

        if (blitz.Charge >= blitz.BombCost)
            DeployBomb(uid, blitz, ResolveAimDirection(uid, msg.AimDirection));
        else
            TryDetonateAll(uid, blitz);
    }

    private Vector2 ResolveAimDirection(EntityUid user, Vector2 aimFromClient)
    {
        if (aimFromClient != Vector2.Zero)
            return Vector2.Normalize(aimFromClient);
        return GetThrowDirection(user);
    }

    private void DeployBomb(EntityUid user, NivalisBlitzerComponent blitz, Vector2 throwDir)
    {
        blitz.Charge -= blitz.BombCost;

        var spawnCoords = Transform(user).Coordinates.Offset(throwDir * 0.4f);
        var bomb = Spawn(BombPrototype, spawnCoords);
        AddComp<NivalisBlitzerBombComponent>(bomb);

        var targetCoords = Transform(user).Coordinates.Offset(throwDir * ThrowDistance);
        _throw.TryThrow(bomb, targetCoords, 7f, user);

        blitz.Bombs.Add(bomb);
        _audio.PlayPvs(new SoundPathSpecifier("/Audio/Weapons/bolathrow.ogg"), user);
    }

    private void TryDetonateAll(EntityUid user, NivalisBlitzerComponent blitz)
    {
        if (blitz.Bombs.Count == 0)
            return;

        var detonated = new List<EntityUid>(blitz.Bombs);
        blitz.Bombs.Clear();

        _audio.PlayPvs(new SoundPathSpecifier("/Audio/Effects/explosion2.ogg"), user);
        foreach (var bomb in detonated)
            DetonateBomb(user, bomb);
    }

    private void DetonateBomb(EntityUid user, EntityUid bomb)
    {
        if (Deleted(bomb) || !TryComp<NivalisBlitzerBombComponent>(bomb, out var bombComp) || bombComp.Detonated)
            return;
        bombComp.Detonated = true;

        var bombCoords = Transform(bomb).Coordinates;
        var epicenter = _transform.GetWorldPosition(bomb);
        _damageable.TryChangeDamage(bomb, new DamageSpecifier
        {
            DamageDict = { ["Structural"] = 1f },
        });
        QueueDel(bomb);

        var targets = new List<EntityUid>();
        var query = EntityQueryEnumerator<DamageableComponent, MobStateComponent>();
        while (query.MoveNext(out var target, out _, out _))
        {
            if (_mobState.IsDead(target))
                continue;

            if ((_transform.GetWorldPosition(target) - epicenter).Length() > BurstRadius)
                continue;

            targets.Add(target);
        }

        var explosiveMult = GetExplosiveMult(user);
        foreach (var target in targets)
        {
            if (_mobState.IsDead(target))
                continue;

            var dist = (_transform.GetWorldPosition(target) - epicenter).Length();
            var falloff = 1f - dist / BurstRadius;
            var dmgMult = explosiveMult * MathF.Max(0.1f, falloff);

            _damageable.TryChangeDamage(target, new DamageSpecifier
            {
                DamageDict = { ["Heat"] = 36f * dmgMult, ["Shock"] = 18f * dmgMult },
            }, origin: user);
            _status.TryAddStatusEffectDuration(target, SharedStunSystem.StunId, TimeSpan.FromSeconds(1f));
        }

        var flash = Spawn(SparkEffect, bombCoords);
        if (TryComp<TimedDespawnComponent>(flash, out var flashDespawn))
            flashDespawn.Lifetime = 0.25f;

        SpawnYellowSparks(bombCoords);
    }

    private void SpawnYellowSparks(EntityCoordinates centre)
    {
        const int filamentCount = 12;
        for (var i = 0; i < filamentCount; i++)
        {
            var dir = _random.NextAngle().ToWorldVec();

            var reach = SparkScatter * _random.NextFloat(0.5f, 1f);

            var line = Spawn(StreakSpark, centre);
            var angle = dir.ToWorldAngle() + AngleOffset90;
            _transform.SetLocalRotation(line, angle);
            _throw.TryThrow(line, centre.Offset(dir * reach), _random.NextFloat(8f, 11f), user: null);
        }
    }

    private Vector2 GetThrowDirection(EntityUid user)
    {
        var angle = _transform.GetWorldRotation(user);
        return angle.ToVec();
    }

    private float GetExplosiveMult(EntityUid user)
    {
        return TryComp<NivalisPerkComponent>(user, out var perk) ? perk.ExplosiveDamageMult : 1f;
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<NivalisBlitzerComponent>();
        while (query.MoveNext(out var uid, out var blitz))
        {
            blitz.Charge = Math.Clamp(blitz.Charge + blitz.RechargeRate * frameTime, 0f, 100f);

            if (blitz.Bombs.Count == 0)
                continue;

            blitz.Bombs.RemoveAll(b => Deleted(b) || !HasComp<NivalisBlitzerBombComponent>(b));
        }
    }
}


