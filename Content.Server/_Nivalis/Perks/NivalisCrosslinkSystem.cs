using System.Linq;
using System.Numerics;
using Content.Server._Nivalis.Status;
using Content.Shared._Nivalis.Perks;
using Content.Shared.Damage;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Systems;
using Content.Shared.Interaction;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Physics;
using Content.Shared.Sprite;
using Content.Shared.StatusEffectNew;
using Content.Shared.Stunnable;
using Content.Shared.Throwing;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Map;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Server._Nivalis.Perks;

public sealed partial class NivalisCrosslinkSystem : EntitySystem
{
    public const string CrosslinkPerk = "Crosslink";

    public const float LinkRadius = 35f;

    private const float ThrowRange = 12f;

    private const float ThrowSpeed = 12f;

    private const float SnareDuration = 3f;

    private const float SnareDamage = 10f;

    private const float SnareCooldown = 10f;

    private const float SnareBleedMultiplier = 2f;

    private const float RecallDamage = 40f;

    private const float RecallStunDuration = 2f;

    private const float RecallRecharge = 10f;

    private const float RecallPathRadius = 0.65f;

    private const float RecallSegmentLength = 0.75f;

    private const float RecallSegmentInterval = 0.01f;

    private static readonly Angle WireSpriteOffset = Angle.FromDegrees(-90);

    private const float WireEndPadding = 0.5f;

    private const float RecallVfxLinger = 0.15f;

    private static readonly EntProtoId KnifePrototype = "NivalisCrosslinkKnife";
    private static readonly EntProtoId WirePrototype = "NivalisCrosslinkWire";
    private static readonly EntProtoId RecallSegmentPrototype = "NivalisCrosslinkRecallSegment";

    private static readonly SoundPathSpecifier ThrowSound = new("/Audio/Weapons/bolathrow.ogg");
    private static readonly SoundPathSpecifier RecallSound = new("/Audio/Weapons/bladeslice.ogg");

    [Dependency] private DamageableSystem _damageable = default!;
    [Dependency] private MobStateSystem _mobState = default!;
    [Dependency] private SharedTransformSystem _transform = default!;
    [Dependency] private StatusEffectsSystem _status = default!;
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private ThrowingSystem _throw = default!;
    [Dependency] private SharedPhysicsSystem _physics = default!;
    [Dependency] private SharedScaleVisualsSystem _scale = default!;
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private NivalisBleedSystem _bleed = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeNetworkEvent<NivalisCrosslinkActionMessage>(OnAction);
        SubscribeLocalEvent<NivalisCrosslinkKnifeComponent, LandEvent>(OnKnifeLanded);
        SubscribeLocalEvent<NivalisCrosslinkKnifeComponent, StopThrowEvent>(OnKnifeStopped);
        SubscribeLocalEvent<NivalisCrosslinkKnifeComponent, ComponentShutdown>(OnKnifeShutdown);
        SubscribeLocalEvent<NivalisCrosslinkWireComponent, ComponentShutdown>(OnWireShutdown);
    }

    private void OnKnifeStopped(Entity<NivalisCrosslinkKnifeComponent> ent, ref StopThrowEvent args)
    {
        PlantKnife(ent);
    }

    private void OnWireShutdown(Entity<NivalisCrosslinkWireComponent> ent, ref ComponentShutdown args)
    {
        if (ent.Comp.KnifeA is { } a && TryComp<NivalisCrosslinkKnifeComponent>(a, out var kac))
            kac.Wires.Remove(ent.Owner);

        if (ent.Comp.KnifeB is { } b && TryComp<NivalisCrosslinkKnifeComponent>(b, out var kbc))
            kbc.Wires.Remove(ent.Owner);
    }

    private void OnAction(NivalisCrosslinkActionMessage msg, EntitySessionEventArgs args)
    {
        if (args.SenderSession.AttachedEntity is not { } uid)
            return;

        if (!TryComp<NivalisPerkComponent>(uid, out var perk) || perk.Perk?.Id != CrosslinkPerk)
            return;

        var cross = EnsureComp<NivalisCrosslinkComponent>(uid);
        if (!cross.Initialised)
        {
            cross.Charge = 100f;
            cross.Initialised = true;
        }

        switch (msg.Action)
        {
            case NivalisCrosslinkAction.PlaceKnife:
                TryPlaceKnife(uid, cross, msg);
                break;
            case NivalisCrosslinkAction.RecallKnife:
                TryRecallKnife(uid, cross, msg);
                break;
        }
    }

    private void TryPlaceKnife(EntityUid user, NivalisCrosslinkComponent cross, NivalisCrosslinkActionMessage msg)
    {
        if (_timing.CurTime < cross.NextThrowTime)
            return;

        if (cross.Charge < cross.KnifeCost - 0.0001f)
            return;

        cross.NextThrowTime = _timing.CurTime + cross.ThrowCooldown;
        cross.Charge = MathF.Max(0f, cross.Charge - cross.KnifeCost);

        var aim = msg.AimDirection;
        if (aim.LengthSquared() < 0.0001f)
            aim = _transform.GetWorldRotation(user).ToWorldVec();
        else
            aim = Vector2.Normalize(aim);

        var userXform = Transform(user);
        var mapId = userXform.MapID;
        var userPos = _transform.GetWorldPosition(userXform);

        var spawnMap = new MapCoordinates(userPos + aim * 0.4f, mapId);
        var knife = Spawn(KnifePrototype, _transform.ToCoordinates(spawnMap));
        var knifeComp = EnsureComp<NivalisCrosslinkKnifeComponent>(knife);
        knifeComp.OwnerPlayer = user;
        knifeComp.Planted = false;

        var clickMap = _transform.ToMapCoordinates(msg.Coordinates);
        var throwDistance = clickMap.MapId == mapId
            ? MathF.Min(ThrowRange, Vector2.Distance(userPos, clickMap.Position))
            : ThrowRange;
        var targetMap = new MapCoordinates(userPos + aim * throwDistance, mapId);
        var targetCoords = _transform.ToCoordinates(targetMap);
        _throw.TryThrow(knife, targetCoords, ThrowSpeed, user, compensateFriction: true, doSpin: false);

        cross.Knives.Add(knife);
        Dirty(user, cross);

        _audio.PlayPvs(ThrowSound, user);
    }

    private void OnKnifeLanded(Entity<NivalisCrosslinkKnifeComponent> ent, ref LandEvent args)
    {
        PlantKnife(ent);
    }

    private void PlantKnife(Entity<NivalisCrosslinkKnifeComponent> ent)
    {
        if (ent.Comp.Planted)
            return;

        ent.Comp.Planted = true;

        _transform.SetWorldRotation(ent.Owner, Angle.Zero);
        if (TryComp<PhysicsComponent>(ent.Owner, out var physics))
            _physics.SetBodyType(ent.Owner, BodyType.Static, body: physics);

        RebuildWires(ent.Comp.OwnerPlayer);
    }

    private void OnKnifeShutdown(Entity<NivalisCrosslinkKnifeComponent> ent, ref ComponentShutdown args)
    {
        foreach (var wire in new List<EntityUid>(ent.Comp.Wires))
            QueueDel(wire);

        if (ent.Comp.OwnerPlayer is { } owner && TryComp<NivalisCrosslinkComponent>(owner, out var cross))
        {
            cross.Knives.Remove(ent.Owner);

            RebuildWires(owner);
        }
    }

    private void RebuildWires(EntityUid? owner)
    {
        if (owner is not { } user || !TryComp<NivalisCrosslinkComponent>(user, out var cross))
            return;

        var knives = new List<EntityUid>();
        foreach (var knife in cross.Knives)
        {
            if (!Deleted(knife) && TryComp<NivalisCrosslinkKnifeComponent>(knife, out var kc) && kc.Planted)
                knives.Add(knife);
        }

        var desired = new HashSet<(EntityUid, EntityUid)>();
        for (var i = 0; i < knives.Count; i++)
        {
            for (var j = i + 1; j < knives.Count; j++)
            {
                var dist = Vector2.Distance(_transform.GetWorldPosition(knives[i]), _transform.GetWorldPosition(knives[j]));
                if (dist <= LinkRadius)
                    desired.Add(Ordered(knives[i], knives[j]));
            }
        }

        var existing = new List<(EntityUid wire, EntityUid a, EntityUid b)>();
        var wireQuery = EntityQueryEnumerator<NivalisCrosslinkWireComponent>();
        while (wireQuery.MoveNext(out var wireUid, out var wire))
        {
            if (wire.KnifeA is not { } a || wire.KnifeB is not { } b)
                continue;

            if (wire.KnifeA is { } ka && TryComp<NivalisCrosslinkKnifeComponent>(ka, out var kac) && kac.OwnerPlayer != user)
                continue;

            if (!desired.Contains(Ordered(a, b)))
            {
                QueueDel(wireUid);
                continue;
            }

            existing.Add((wireUid, a, b));
        }

        var existingPairs = new HashSet<(EntityUid, EntityUid)>();
        foreach (var (_, a, b) in existing)
            existingPairs.Add(Ordered(a, b));

        foreach (var pair in desired)
        {
            if (existingPairs.Contains(pair))
                continue;

            CreateWire(user, pair.Item1, pair.Item2);
        }
    }

    private static (EntityUid, EntityUid) Ordered(EntityUid a, EntityUid b)
    {
        return a.GetHashCode() <= b.GetHashCode() ? (a, b) : (b, a);
    }

    private void CreateWire(EntityUid owner, EntityUid a, EntityUid b)
    {
        if (Deleted(a) || Deleted(b))
            return;

        var wire = Spawn(WirePrototype, Transform(a).Coordinates);
        var wireComp = EnsureComp<NivalisCrosslinkWireComponent>(wire);
        wireComp.KnifeA = a;
        wireComp.KnifeB = b;
        wireComp.OwnerPlayer = owner;

        if (TryComp<NivalisCrosslinkKnifeComponent>(a, out var kac))
            kac.Wires.Add(wire);
        if (TryComp<NivalisCrosslinkKnifeComponent>(b, out var kbc))
            kbc.Wires.Add(wire);

        UpdateWireVisual(wire, wireComp);
    }

    private void UpdateWireVisual(EntityUid wire, NivalisCrosslinkWireComponent comp)
    {
        if (Deleted(wire) || comp.KnifeA is not { } a || comp.KnifeB is not { } b)
            return;

        if (Deleted(a) || Deleted(b))
            return;

        var posA = _transform.GetWorldPosition(a);
        var posB = _transform.GetWorldPosition(b);
        var mapId = Transform(a).MapID;

        LayoutWireSprite(wire, posA, posB, mapId, WireEndPadding);
    }

    private void TryRecallKnife(EntityUid user, NivalisCrosslinkComponent cross, NivalisCrosslinkActionMessage msg)
    {
        if (_timing.CurTime < cross.NextRecallTime)
            return;

        var aim = msg.AimDirection;
        if (aim.LengthSquared() < 0.0001f)
            aim = _transform.GetWorldRotation(user).ToWorldVec();
        else
            aim = Vector2.Normalize(aim);

        var userPos = _transform.GetWorldPosition(user);

        EntityUid? best = null;
        var bestScore = float.MaxValue;

        EntityUid? nearest = null;
        var nearestDistance = float.MaxValue;

        foreach (var knife in cross.Knives)
        {
            if (Deleted(knife))
                continue;

            var knifePos = _transform.GetWorldPosition(knife);
            var toKnife = knifePos - userPos;
            var distance = toKnife.Length();
            if (distance < 0.001f || distance > LinkRadius)
                continue;

            if (distance < nearestDistance)
            {
                nearestDistance = distance;
                nearest = knife;
            }

            var dot = Vector2.Dot(toKnife / distance, aim);
            if (dot < 0.35f)
                continue;

            var score = distance * (1.1f - dot);
            if (score < bestScore)
            {
                bestScore = score;
                best = knife;
            }
        }

        var target = best ?? nearest;
        if (target is not { } targetUid)
            return;

        cross.NextRecallTime = _timing.CurTime + cross.RecallCooldown;
        cross.Charge = Math.Clamp(cross.Charge + RecallRecharge, 0f, 100f);
        Dirty(user, cross);

        RecallKnife(user, targetUid, cross);

        _audio.PlayPvs(RecallSound, user);
    }

    private void RecallKnife(EntityUid user, EntityUid knife, NivalisCrosslinkComponent cross)
    {
        var userPos = _transform.GetWorldPosition(user);
        var knifePos = _transform.GetWorldPosition(knife);
        var mapId = Transform(knife).MapID;

        var path = userPos - knifePos;
        var length = path.Length();
        if (length > 0.001f)
        {
            var hits = FindEntitiesAlongPath(knifePos, path, length, mapId, user);
            foreach (var victim in hits)
            {
                if (_mobState.IsDead(victim))
                    continue;

                _damageable.TryChangeDamage(victim, new DamageSpecifier
                {
                    DamageDict = { ["Slash"] = RecallDamage },
                }, origin: user);

                _status.TryAddStatusEffectDuration(victim, SharedStunSystem.StunId, TimeSpan.FromSeconds(RecallStunDuration));
            }
        }

        StartRecallVfx(knifePos, userPos, mapId);

        cross.Knives.Remove(knife);
        Dirty(user, cross);
        QueueDel(knife);
    }

    private void StartRecallVfx(Vector2 from, Vector2 to, MapId mapId)
    {
        var delta = to - from;
        var distance = delta.Length();
        if (distance < 0.001f)
            return;

        var segmentCount = Math.Max(1, (int) MathF.Ceiling(distance / RecallSegmentLength));

        var vfx = Spawn((string?) null);
        _transform.SetMapCoordinates(vfx, new MapCoordinates(from, mapId));
        var comp = EnsureComp<NivalisCrosslinkRecallVfxComponent>(vfx);
        comp.From = new MapCoordinates(from, mapId);
        comp.To = new MapCoordinates(to, mapId);
        comp.StartTime = _timing.CurTime;
        comp.SegmentInterval = TimeSpan.FromSeconds(RecallSegmentInterval);
        comp.SegmentCount = segmentCount;
        comp.Revealed = 0;
    }

    private void UpdateRecallVfx(Entity<NivalisCrosslinkRecallVfxComponent> ent, TimeSpan now)
    {
        var comp = ent.Comp;
        var elapsed = now - comp.StartTime;

        var desired = Math.Min(comp.SegmentCount, (int) (elapsed.TotalSeconds / RecallSegmentInterval) + 1);
        while (comp.Revealed < desired)
        {
            var index = comp.Revealed;
            comp.Revealed++;

            var t0 = index / (float) comp.SegmentCount;
            var t1 = (index + 1) / (float) comp.SegmentCount;

            var p0 = Vector2.Lerp(comp.From.Position, comp.To.Position, t0);
            var p1 = Vector2.Lerp(comp.From.Position, comp.To.Position, t1);

            var segment = Spawn(RecallSegmentPrototype, _transform.ToCoordinates(new MapCoordinates(p0, comp.From.MapId)));
            comp.Segments.Add(segment);
            LayoutWireSprite(segment, p0, p1, comp.From.MapId, WireEndPadding);
        }

        if (comp.Revealed >= comp.SegmentCount)
        {
            var finished = comp.StartTime
                           + TimeSpan.FromSeconds(RecallSegmentInterval * comp.SegmentCount)
                           + TimeSpan.FromSeconds(RecallVfxLinger);

            if (now >= finished)
            {
                foreach (var segment in comp.Segments)
                {
                    if (!Deleted(segment))
                        QueueDel(segment);
                }

                QueueDel(ent.Owner);
            }
        }
    }

    private void LayoutWireSprite(EntityUid sprite, Vector2 a, Vector2 b, MapId mapId, float padding)
    {
        var arm = b - a;
        var length = arm.Length();
        if (length < 0.001f)
            return;

        var dir = arm / length;

        var start = a - dir * padding;
        var end = b + dir * padding;

        var mid = (start + end) / 2f;
        _transform.SetMapCoordinates(sprite, new MapCoordinates(mid, mapId));
        _transform.SetWorldRotation(sprite, arm.ToWorldAngle() + WireSpriteOffset);
        _scale.SetSpriteScale(sprite, new Vector2(length + padding * 2f, 1f));
    }

    private List<EntityUid> FindEntitiesAlongPath(Vector2 origin, Vector2 delta, float length, MapId mapId, EntityUid ignore)
    {
        var results = new HashSet<EntityUid>();
        var dir = delta / length;

        var query = EntityQueryEnumerator<DamageableComponent, MobStateComponent, TransformComponent>();
        while (query.MoveNext(out var ent, out _, out _, out var xform))
        {
            if (ent == ignore || _mobState.IsDead(ent))
                continue;

            if (xform.MapID != mapId)
                continue;

            var entPos = _transform.GetWorldPosition(xform);
            var toEnt = entPos - origin;
            var along = Vector2.Dot(toEnt, dir);
            if (along < 0f || along > length)
                continue;

            var closest = origin + dir * along;
            if (Vector2.Distance(closest, entPos) <= RecallPathRadius)
                results.Add(ent);
        }

        return results.ToList();
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var now = _timing.CurTime;

        var crossQuery = EntityQueryEnumerator<NivalisCrosslinkComponent>();
        while (crossQuery.MoveNext(out var uid, out var cross))
        {
            if (!cross.Initialised)
                continue;

            if (cross.Charge >= 100f)
                continue;

            cross.Charge = Math.Clamp(cross.Charge + cross.RechargeRate * frameTime, 0f, 100f);
            Dirty(uid, cross);
        }

        var wireQuery = EntityQueryEnumerator<NivalisCrosslinkWireComponent>();
        while (wireQuery.MoveNext(out var wireUid, out var wire))
        {
            if (Deleted(wireUid))
                continue;

            if (wire.KnifeA is not { } a || wire.KnifeB is not { } b || Deleted(a) || Deleted(b))
            {
                QueueDel(wireUid);
                continue;
            }

            UpdateWireVisual(wireUid, wire);
            ProcessWireSnare(wireUid, wire, a, b, now);
        }

        var vfxQuery = EntityQueryEnumerator<NivalisCrosslinkRecallVfxComponent>();
        while (vfxQuery.MoveNext(out var vfxUid, out var vfx))
        {
            UpdateRecallVfx((vfxUid, vfx), now);
        }
    }

    private void ProcessWireSnare(EntityUid wireUid, NivalisCrosslinkWireComponent wire, EntityUid a, EntityUid b, TimeSpan now)
    {
        if (wire.SnaredTargets.Count > 0)
        {
            var expired = new List<EntityUid>();
            foreach (var (target, readyAgain) in wire.SnaredTargets)
            {
                if (Deleted(target) || now >= readyAgain)
                    expired.Add(target);
            }

            foreach (var target in expired)
                wire.SnaredTargets.Remove(target);
        }

        var posA = _transform.GetWorldPosition(a);
        var posB = _transform.GetWorldPosition(b);
        var seg = posB - posA;
        var segLen = seg.Length();
        if (segLen < 0.001f)
            return;

        var mapId = Transform(a).MapID;
        var dir = seg / segLen;

        var query = EntityQueryEnumerator<DamageableComponent, MobStateComponent, TransformComponent>();
        while (query.MoveNext(out var ent, out _, out _, out var xform))
        {
            if (xform.MapID != mapId || _mobState.IsDead(ent))
                continue;

            if (wire.OwnerPlayer is { } owner && ent == owner)
                continue;

            if (wire.SnaredTargets.ContainsKey(ent))
                continue;

            var entPos = _transform.GetWorldPosition(xform);
            var toEnt = entPos - posA;
            var along = Vector2.Dot(toEnt, dir);
            if (along < 0f || along > segLen)
                continue;

            var closest = posA + dir * along;
            if (Vector2.Distance(closest, entPos) > 0.55f)
                continue;

            SnareTarget(wire, ent, now);
        }
    }

    public bool IsSnareOnCooldown(EntityUid wire, EntityUid target)
    {
        return TryComp<NivalisCrosslinkWireComponent>(wire, out var comp) && comp.SnaredTargets.ContainsKey(target);
    }

    private void SnareTarget(NivalisCrosslinkWireComponent wire, EntityUid target, TimeSpan now)
    {
        wire.SnaredTargets[target] = now + TimeSpan.FromSeconds(SnareCooldown);

        _status.TryAddStatusEffectDuration(target, SharedStunSystem.StunId, TimeSpan.FromSeconds(SnareDuration));

        _damageable.TryChangeDamage(target, new DamageSpecifier
        {
            DamageDict = { ["Slash"] = SnareDamage },
        });

        _bleed.TryApplyBleed(target, target, 1f * SnareBleedMultiplier);
    }
}
