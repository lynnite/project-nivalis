using Content.Shared._Nivalis.GameTicking.Components;
using Content.Shared._Nivalis.Survivor.Components;
using Content.Shared.Damage;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Prototypes;
using Content.Shared.Damage.Systems;
using Content.Shared.FixedPoint;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;
using System.Collections.Generic;

namespace Content.Server._Nivalis.GameTicking.Rules;

public sealed partial class NivalisStormSystem : EntitySystem
{
    private static readonly EntProtoId CloudProto = "NivalisSmokeCloud";

    [Dependency] private EntityLookupSystem _lookup = default!;
    [Dependency] private DamageableSystem _damageable = default!;
    [Dependency] private SharedTransformSystem _xforms = default!;
    [Dependency] private IPrototypeManager _protos = default!;
    [Dependency] private IGameTiming _timing = default!;

    private EntityQuery<DamageableComponent> _damageQuery = default!;
    private EntityQuery<NivalisSurvivorComponent> _survivorQuery = default!;
    private readonly Dictionary<EntityUid, EntityUid> _clouds = new();

    public override void Initialize()
    {
        base.Initialize();

        _damageQuery = GetEntityQuery<DamageableComponent>();
        _survivorQuery = GetEntityQuery<NivalisSurvivorComponent>();

        SubscribeLocalEvent<NivalisSurvivalPhaseChangedEvent>(OnPhaseChanged);
    }

    private void OnPhaseChanged(ref NivalisSurvivalPhaseChangedEvent args)
    {
        var active = args.NewPhase == NivalisSurvivalPhase.Storm;

        var query = EntityQueryEnumerator<NivalisStormZoneComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out var zone, out var xform))
        {
            SetActive((uid, zone), active);
            EnsureSmokeCloud(uid, xform);
        }
    }

    private void EnsureSmokeCloud(EntityUid uid, TransformComponent xform)
    {
        if (_clouds.ContainsKey(uid))
            return;

        var cloud = Spawn(CloudProto, _xforms.GetMapCoordinates((uid, xform)));
        _xforms.SetParent(cloud, uid);
        _clouds[uid] = cloud;
    }

    private void SetActive(Entity<NivalisStormZoneComponent> ent, bool active)
    {
        if (ent.Comp.Active == active)
            return;

        ent.Comp.Active = active;
        ent.Comp.NextTick = _timing.CurTime + TimeSpan.FromSeconds(ent.Comp.DamageInterval);
        Dirty(ent);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<NivalisStormZoneComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out var zone, out var xform))
        {
            if (!zone.Active)
                continue;

            if (_timing.CurTime < zone.NextTick)
                continue;

            zone.NextTick = _timing.CurTime + TimeSpan.FromSeconds(zone.DamageInterval);
            Dirty(uid, zone);

            DamageSurvivorsInZone((uid, zone), xform);
        }
    }

    private void DamageSurvivorsInZone(Entity<NivalisStormZoneComponent> ent, TransformComponent xform)
    {
        if (xform.MapID == MapId.Nullspace)
            return;

        var worldPos = _xforms.GetWorldPosition(xform);
        var targets = _lookup.GetEntitiesInRange(xform.MapID, worldPos, ent.Comp.Radius);

        if (targets.Count == 0)
            return;

        var damage = new DamageSpecifier(
            _protos.Index(ent.Comp.DamageType),
            FixedPoint2.New(ent.Comp.StormDamagePerSecond));

        foreach (var target in targets)
        {
            if (!_survivorQuery.HasComp(target))
                continue;

            if (!_damageQuery.TryGetComponent(target, out var damageable))
                continue;

            _damageable.ChangeDamage((target, damageable), damage, true, origin: ent);
        }
    }
}

