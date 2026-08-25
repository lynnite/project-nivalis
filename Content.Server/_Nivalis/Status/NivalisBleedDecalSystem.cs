using System.Numerics;
using Content.Server.Decals;
using Content.Shared._Nivalis.Status;
using Content.Shared.Mobs.Components;
using Content.Shared.Movement.Systems;
using Content.Shared.StatusEffectNew;
using Robust.Shared.Map.Components;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server._Nivalis.Status;

public sealed partial class NivalisBleedDecalSystem : EntitySystem
{
    public static readonly EntProtoId BleedEffect = "StatusEffectNivalisBleed";
    public static readonly EntProtoId ArteryEffect = "StatusEffectNivalisArtery";

    private static readonly Vector2 BleedSpawnInterval = new(3f, 8f);

    private const float SlowPerSpawn = 0.005f;

    private const float MaxSlow = 0.10f;

    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private DecalSystem _decals = default!;
    [Dependency] private SharedMapSystem _map = default!;
    [Dependency] private StatusEffectsSystem _status = default!;
    [Dependency] private MovementSpeedModifierSystem _movementSpeed = default!;

    private static readonly string[] BloodDecalIds =
    {
        "NivalisBloodFloor1",
        "NivalisBloodFloor2",
        "NivalisBloodFloor3",
        "NivalisBloodFloor4",
        "NivalisBloodFloor5",
        "NivalisBloodFloor6",
        "NivalisBloodFloor7",
    };

    private EntityQuery<MobStateComponent> _mobQuery = default!;

    public override void Initialize()
    {
        base.Initialize();

        _mobQuery = GetEntityQuery<MobStateComponent>();

        SubscribeLocalEvent<NivalisBleedDecalComponent, RefreshMovementSpeedModifiersEvent>(OnRefreshSpeed);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var bleeding = new HashSet<EntityUid>();
        foreach (var (uid, _) in EntityQueryEnumerator<NivalisBleedActiveComponent>())
            bleeding.Add(uid);
        foreach (var (uid, _) in EntityQueryEnumerator<NivalisArteryActiveComponent>())
            bleeding.Add(uid);

        foreach (var uid in bleeding)
        {
            if (!_mobQuery.HasComp(uid))
                continue;

            var decal = EnsureComp<NivalisBleedDecalComponent>(uid);
            if (decal.NextSpawn == TimeSpan.Zero)
            {
                decal.NextSpawn = _timing.CurTime;
                Dirty(uid, decal);
            }
        }

        var toRemove = new List<EntityUid>();
        var query = EntityQueryEnumerator<NivalisBleedDecalComponent>();
        while (query.MoveNext(out var uid, out var decal))
        {
            if (!bleeding.Contains(uid))
            {
                if (decal.SpawnCount != 0)
                {
                    decal.SpawnCount = 0;
                    Dirty(uid, decal);
                    _movementSpeed.RefreshMovementSpeedModifiers(uid);
                }
                toRemove.Add(uid);
                continue;
            }

            if (_timing.CurTime < decal.NextSpawn)
                continue;

            SpawnBloodDecal(uid);

            decal.SpawnCount++;
            decal.NextSpawn = GetNextSpawnTime(uid);
            Dirty(uid, decal);
            _movementSpeed.RefreshMovementSpeedModifiers(uid);
        }

        foreach (var uid in toRemove)
            RemComp<NivalisBleedDecalComponent>(uid);
    }

    private void OnRefreshSpeed(EntityUid uid, NivalisBleedDecalComponent comp, ref RefreshMovementSpeedModifiersEvent args)
    {
        var slow = Math.Min(comp.SpawnCount * SlowPerSpawn, MaxSlow);
        var modifier = 1f - slow;
        args.ModifySpeed(modifier);
    }

    private TimeSpan GetNextSpawnTime(EntityUid uid)
    {
        var seconds = _random.NextFloat(BleedSpawnInterval.X, BleedSpawnInterval.Y);
        if (_status.HasStatusEffect(uid, ArteryEffect))
            seconds /= 2f;

        return _timing.CurTime + TimeSpan.FromSeconds(seconds);
    }

    private void SpawnBloodDecal(EntityUid uid)
    {
        var xform = Transform(uid);
        if (xform.GridUid is not { } gridUid || !TryComp<MapGridComponent>(gridUid, out var grid))
            return;

        var tile = _map.TileIndicesFor(gridUid, grid, xform.Coordinates);
        var coords = _map.GridTileToLocal(gridUid, grid, tile);

        var id = BloodDecalIds[_random.Next(BloodDecalIds.Length)];
        _decals.TryAddDecal(id, coords, out _);
    }
}


