using System.Numerics;
using Content.Shared._Nivalis.Perks;
using Content.Shared.Damage;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Systems;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Sprite;
using Content.Shared.StatusEffectNew;
using Content.Shared.Stunnable;
using Robust.Server.Player;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Configuration;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Physics.Components;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;
using PhysicsSystem = Robust.Shared.Physics.Systems.SharedPhysicsSystem;

namespace Content.Server._Nivalis.Perks;

public sealed partial class NivalisArbiterSystem : EntitySystem
{
    public const string ArbiterPerk = "Arbiter";

    private const float ShortFireDelay = 0.8f;

    private const float ReadyCueAt = 0.55f;

    private const float LongshotWindUp = 0.35f;

    private const float ShortDistance = 2.2f;
    private const float ShortAoe = 2.4f;
    private const float ShortDamage = 40f;

    private const float LongDistance = 6.0f;
    private const float LongAoe = 4.2f;
    private const float LongDamage = 90f;

    private const float RecoilShort = 7f;
    private const float RecoilLong = 14f;

    private static readonly EntProtoId MuzzleEffect = "NivalisArbiterMuzzle";
    private static readonly EntProtoId ImpactEffect = "NivalisArbiterImpact";
    private static readonly EntProtoId BeamEffect = "NivalisArbiterBeam";

    [Dependency] private DamageableSystem _damageable = default!;
    [Dependency] private MobStateSystem _mobState = default!;
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private StatusEffectsSystem _status = default!;
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private PhysicsSystem _physics = default!;
    [Dependency] private IConfigurationManager _cfg = default!;
    [Dependency] private SharedTransformSystem _xform = default!;
    [Dependency] private SharedScaleVisualsSystem _scaleVisuals = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeNetworkEvent<NivalisPerkAbilityPressedMessage>(OnAbilityPressed);
    }

    private bool TryGetArbiter(EntityUid uid, out Entity<NivalisArbiterComponent> arb)
    {
        arb = default;
        if (!TryComp<NivalisPerkComponent>(uid, out var perk) || perk.Perk?.Id != ArbiterPerk)
            return false;

        arb = (uid, EnsureComp<NivalisArbiterComponent>(uid));
        return true;
    }

    private void OnAbilityPressed(NivalisPerkAbilityPressedMessage msg, EntitySessionEventArgs args)
    {
        if (args.SenderSession.AttachedEntity is not { } uid)
            return;

        if (!TryGetArbiter(uid, out var arb))
            return;

        if (!arb.Comp.Initialised)
        {
            arb.Comp.Charge = 100f;
            arb.Comp.Initialised = true;
        }

        if (arb.Comp.Charge < arb.Comp.BlastCost - 0.0001f)
            return;

        var now = _timing.CurTime;
        var aim = NormalizeOrUnitX(msg.AimDirection);

        if (arb.Comp.Ready && !arb.Comp.CycleResolved)
        {
            arb.Comp.Ready = false;
            arb.Comp.CycleResolved = true;
            arb.Comp.LongReady = true;
            arb.Comp.LongAim = aim;
            arb.Comp.LongWindupAt = now;
            return;
        }

        arb.Comp.Ready = true;
        arb.Comp.ReadyAt = now;
        arb.Comp.ShortAim = aim;
        arb.Comp.ReadyCueSent = false;
        arb.Comp.CycleResolved = false;
        arb.Comp.LongReady = false;
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var now = _timing.CurTime;
        var query = EntityQueryEnumerator<NivalisArbiterComponent>();
        while (query.MoveNext(out var uid, out var arb))
        {
            if (Deleted(uid) || Terminating(uid))
                continue;

            arb.Charge = Math.Clamp(arb.Charge + arb.RechargeRate * frameTime, 0f, 100f);

            if (arb.LongReady && now - arb.LongWindupAt >= TimeSpan.FromSeconds(LongshotWindUp))
            {
                arb.LongReady = false;
                arb.Ready = false;
                arb.CycleResolved = true;
                FireLongshot(uid, arb);
                continue;
            }

            if (arb.Ready && !arb.CycleResolved &&
                now - arb.ReadyAt >= TimeSpan.FromSeconds(ShortFireDelay))
            {
                arb.Ready = false;
                arb.CycleResolved = true;
                FireShortshot(uid, arb);

                continue;
            }

            if (arb.Ready && !arb.ReadyCueSent &&
                now - arb.ReadyAt >= TimeSpan.FromSeconds(ReadyCueAt))
            {
                arb.ReadyCueSent = true;
                SendFlash(uid, 255, 255, 64, 0.08f, 0.3f);
            }
        }
    }

    private static Vector2 NormalizeOrUnitX(Vector2 v)
    {
        return v.LengthSquared() > 0.0001f ? Vector2.Normalize(v) : Vector2.UnitX;
    }

    private void FireShortshot(EntityUid uid, NivalisArbiterComponent arb)
    {
        arb.Charge = MathF.Max(0f, arb.Charge - arb.BlastCost);
        _audio.PlayPvs(new SoundPathSpecifier("/Audio/Weapons/Guns/Gunshots/shotgun.ogg"), uid);
        FireBlast(uid, arb.ShortAim, ShortDistance, ShortAoe, ShortDamage, RecoilShort);
    }

    private void FireLongshot(EntityUid uid, NivalisArbiterComponent arb)
    {
        arb.Charge = MathF.Max(0f, arb.Charge - arb.BlastCost);
        _audio.PlayPvs(new SoundPathSpecifier("/Audio/Effects/explosion6.ogg"), uid);
        SendFlash(uid, 255, 255, 255, 0.15f, 0.35f);
        FireBlast(uid, arb.LongAim, LongDistance, LongAoe, LongDamage, RecoilLong);
    }

    private void FireBlast(EntityUid user, Vector2 aim, float distance, float aoe,
        float damage, float recoil)
    {
        if (!Exists(user))
            return;

        var mapCoords = _xform.ToMapCoordinates(Transform(user).Coordinates);
        var origin = mapCoords.Position;
        var mapId = mapCoords.MapId;

        var muzzlePos = origin + aim * 0.5f;
        var impactPos = origin + aim * distance;

        var muzzle = Spawn(MuzzleEffect, new MapCoordinates(muzzlePos, mapId));
        var impact = Spawn(ImpactEffect, new MapCoordinates(impactPos, mapId));

        if (aim.LengthSquared() > 0.0001f)
        {
            var shotAngle = aim.ToAngle();
            if (muzzle.IsValid())
                _xform.SetWorldRotation(muzzle, shotAngle);
            if (impact.IsValid())
                _xform.SetWorldRotation(impact, shotAngle);
        }

        var arm = impactPos - muzzlePos;
        if (arm.LengthSquared() > 0.0001f)
        {
            var dip = arm.Normalized();
            var beam = Spawn(BeamEffect, new MapCoordinates(muzzlePos + arm / 2f, mapId));
            if (beam.IsValid())
            {
                _xform.SetWorldRotation(beam, dip.ToAngle());
                _scaleVisuals.SetSpriteScale(beam, new Vector2(arm.Length(), 1f));
            }
        }

        DealBlast(user, impactPos, aoe, damage);

        if (TryComp<PhysicsComponent>(user, out var body))
        {
            _physics.ApplyLinearImpulse(user, -aim * recoil * body.Mass, body: body);
        }
    }

    private void DealBlast(EntityUid user, Vector2 epicenter, float radius, float damage)
    {
        var query = EntityQueryEnumerator<DamageableComponent, MobStateComponent>();
        while (query.MoveNext(out var ent, out _, out _))
        {
            if (ent == user || _mobState.IsDead(ent))
                continue;

            var dist = (_xform.GetMapCoordinates(ent).Position - epicenter).Length();
            if (dist > radius)
                continue;

            var falloff = MathF.Max(0.25f, 1f - dist / radius);
            _damageable.TryChangeDamage(ent, new DamageSpecifier
            {
                DamageDict = { ["Explosive"] = damage * falloff },
            }, origin: user, ignoreResistances: false);

            _status.TryAddStatusEffectDuration(ent, SharedStunSystem.StunId, TimeSpan.FromSeconds(1f));
        }
    }

    private void SendFlash(EntityUid uid, byte red, byte green, byte blue, float holdTime, float fadeTime)
    {
        var filter = Filter.Empty().AddPlayersByPvs(uid, entityManager: EntityManager,
            playerMan: IoCManager.Resolve<IPlayerManager>(), cfgMan: _cfg);
        RaiseNetworkEvent(new NivalisArbiterFlashMessage(GetNetEntity(uid), red, green, blue, holdTime, fadeTime), filter);
    }
}
