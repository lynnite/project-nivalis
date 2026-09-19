using System.Numerics;
using Content.Server.Decals;
using Content.Shared._Nivalis.Melee;
using Content.Shared._Nivalis.Health;
using Content.Shared._Nivalis.Weapons;
using Content.Shared.Damage;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Systems;
using Content.Shared.Humanoid;
using Content.Shared.Inventory;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Maths;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server._Nivalis.Weapons;

public sealed partial class NivalisDecimatorSystem : EntitySystem
{
    private static readonly TimeSpan MarkDuration = TimeSpan.FromSeconds(0.5);

    private const int BloodDecalCount = 6;
    private static readonly TimeSpan BloodDecalInterval = TimeSpan.FromSeconds(0.1);

    private const int BloodDecalWidth = 3;
    private const int BloodDecalHeight = 1;

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

    [Dependency] private DamageableSystem _damageable = default!;
    [Dependency] private SharedTransformSystem _transform = default!;
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private SharedHideableHumanoidLayersSystem _hideableLayers = default!;
    [Dependency] private InventorySystem _inventory = default!;
    [Dependency] private DecalSystem _decals = default!;
    [Dependency] private SharedMapSystem _map = default!;
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private IRobustRandom _random = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<NivalisMeleeHitEvent>(OnMeleeHit);
        SubscribeLocalEvent<NivalisDecimatorComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<MobStateChangedEvent>(OnMobStateChanged);
    }

    private void OnMapInit(EntityUid uid, NivalisDecimatorComponent comp, MapInitEvent args)
    {
        if (comp.BaseAttackRate <= 0f && TryComp<NivalisMeleeComponent>(uid, out var melee))
            comp.BaseAttackRate = melee.AttackRate;
    }

    private void OnMeleeHit(NivalisMeleeHitEvent args)
    {
        if (!TryComp<NivalisDecimatorComponent>(args.Weapon, out var decimator) ||
            !TryComp<NivalisMeleeComponent>(args.Weapon, out var melee))
        {
            return;
        }

        if (!args.Heavy)
        {
            if (args.IsHit)
                GrowMomentum(args.Weapon, decimator, melee);

            return;
        }

        if (!args.IsHit)
        {
            ResetMomentum(args.Weapon, decimator, melee);
            return;
        }

        GrowMomentum(args.Weapon, decimator, melee);

        Detonate(args.User, args.HitEntities, decimator);
    }

    private void Detonate(EntityUid user, List<EntityUid> hitEntities, NivalisDecimatorComponent decimator)
    {
        if (hitEntities.Count == 0)
            return;

        var epicenterUid = hitEntities[0];
        if (TerminatingOrDeleted(epicenterUid))
            return;

        var epicenter = _transform.GetWorldPosition(epicenterUid);
        var mapId = Transform(epicenterUid).MapID;

        if (decimator.ExplosionSound != null)
            _audio.PlayPvs(decimator.ExplosionSound, epicenterUid);

        var radius = decimator.ExplosionRadius;

        var toProcess = new List<EntityUid>();
        var query = EntityQueryEnumerator<DamageableComponent, MobStateComponent, TransformComponent>();
        while (query.MoveNext(out var target, out _, out _, out var xform))
        {
            if (target == user)
                continue;

            if (xform.MapID != mapId)
                continue;

            var targetPos = _transform.GetWorldPosition(xform);
            if ((targetPos - epicenter).Length() > radius)
                continue;

            toProcess.Add(target);
        }

        foreach (var target in toProcess)
        {
            if (TerminatingOrDeleted(target))
                continue;

            var mark = EnsureComp<NivalisDecimatorMarkComponent>(target);
            mark.ExpiresAt = _timing.CurTime + MarkDuration;

            var targetPos = _transform.GetWorldPosition(target);
            var dist = (targetPos - epicenter).Length();
            var falloff = radius <= 0f
                ? 1f
                : MathHelper.Lerp(decimator.ExplosionEdgeMultiplier, 1f, 1f - Math.Clamp(dist / radius, 0f, 1f));

            var damage = decimator.ExplosionDamage * falloff;
            if (damage.GetTotal() <= 0f)
                continue;

            _damageable.TryChangeDamage(target, damage, origin: user);
        }
    }

    private void OnMobStateChanged(MobStateChangedEvent args)
    {
        if (args.NewMobState != MobState.Critical && args.NewMobState != MobState.Dead)
            return;

        var uid = args.Target;
        if (!TryComp<NivalisDecimatorMarkComponent>(uid, out var mark))
            return;

        RemComp<NivalisDecimatorMarkComponent>(uid);

        if (_timing.CurTime <= mark.ExpiresAt)
            TryDecapitate(uid);
    }

    private static readonly HumanoidVisualLayers[] HeadLayers =
    {
        HumanoidVisualLayers.Head,
        HumanoidVisualLayers.HeadSide,
        HumanoidVisualLayers.HeadTop,
        HumanoidVisualLayers.Snout,
        HumanoidVisualLayers.SnoutCover,
        HumanoidVisualLayers.Eyes,
        HumanoidVisualLayers.Hair,
        HumanoidVisualLayers.FacialHair,
    };

    private void TryDecapitate(EntityUid target)
    {
        if (HasComp<NivalisDecapitatedComponent>(target))
            return;

        var decapitated = EnsureComp<NivalisDecapitatedComponent>(target);
        Dirty(target, decapitated);

        if (TryComp<HideableHumanoidLayersComponent>(target, out _))
        {
            foreach (var layer in HeadLayers)
                _hideableLayers.SetLayerOcclusion(target, layer, true, SlotFlags.NECK);
        }

        DeleteHeadwear(target);

        SpawnBloodBurst(target);
    }

    private void DeleteHeadwear(EntityUid target)
    {
        var headgear = new List<EntityUid>();

        if (TryComp<InventoryComponent>(target, out var inventory))
        {
            foreach (var slot in inventory.Slots)
            {
                if ((slot.SlotFlags & SlotFlags.HEAD) == 0)
                    continue;

                if (_inventory.TryGetSlotEntity(target, slot.Name, out var item, inventory))
                    headgear.Add(item.Value);
            }
        }

        foreach (var item in headgear)
        {
            if (!TerminatingOrDeleted(item))
                QueueDel(item);
        }
    }

    private static readonly Vector2 HeadDirection = new(-1f, 0f);

    private void SpawnBloodBurst(EntityUid target)
    {
        var splurt = Spawn("NivalisBloodSplurt", Transform(target).Coordinates);

        var comp = EnsureComp<NivalisBloodSplurtComponent>(splurt);
        comp.Seed = _random.Next();
        comp.Direction = HeadDirection;
        Dirty(splurt, comp);

        var decals = EnsureComp<NivalisBloodDecalSpawnerComponent>(splurt);
        decals.Remaining = BloodDecalCount;
        decals.Interval = (float)BloodDecalInterval.TotalSeconds;
        decals.Direction = comp.Direction;
        decals.Width = BloodDecalWidth;
        decals.Height = BloodDecalHeight;
        decals.NextSpawn = _timing.CurTime;
        Dirty(splurt, decals);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<NivalisBloodDecalSpawnerComponent>();
        while (query.MoveNext(out var uid, out var spawner))
        {
            if (_timing.CurTime < spawner.NextSpawn)
                continue;

            TryPlaceBloodDecal(uid, spawner);

            spawner.Remaining--;
            spawner.NextSpawn = _timing.CurTime + TimeSpan.FromSeconds(spawner.Interval);

            if (spawner.Remaining <= 0)
                RemComp<NivalisBloodDecalSpawnerComponent>(uid);
        }
    }

    private void TryPlaceBloodDecal(EntityUid uid, NivalisBloodDecalSpawnerComponent spawner)
    {
        var xform = Transform(uid);
        if (xform.GridUid is not { } gridUid || !TryComp<MapGridComponent>(gridUid, out var grid))
            return;

        var centreTile = _map.TileIndicesFor(gridUid, grid, xform.Coordinates);

        var dir = spawner.Direction;
        if (dir.LengthSquared() < 0.0001f)
            dir = new Vector2(-1f, 0f);
        dir = Vector2.Normalize(dir);

        Vector2i outDir;
        Vector2i acrossDir;
        if (MathF.Abs(dir.X) >= MathF.Abs(dir.Y))
        {
            outDir = new Vector2i(Math.Sign(dir.X), 0);
            acrossDir = new Vector2i(0, 1);
        }
        else
        {
            outDir = new Vector2i(0, Math.Sign(dir.Y));
            acrossDir = new Vector2i(1, 0);
        }

        var outStep = _random.Next(1, Math.Max(1, spawner.Width) + 1);
        var acrossHalf = (Math.Max(1, spawner.Height) - 1) / 2;
        var acrossStep = _random.Next(-acrossHalf, acrossHalf + 1);

        TryAddDecalAt(gridUid, grid, centreTile + outDir * outStep + acrossDir * acrossStep);
    }

    private void TryAddDecalAt(EntityUid gridUid, MapGridComponent grid, Vector2i tile)
    {
        var coords = _map.GridTileToLocal(gridUid, grid, tile);
        var id = BloodDecalIds[_random.Next(BloodDecalIds.Length)];
        _decals.TryAddDecal(id, coords, out _);
    }

    private void GrowMomentum(EntityUid weaponUid, NivalisDecimatorComponent decimator, NivalisMeleeComponent melee)
    {
        var baseRate = decimator.BaseAttackRate > 0f ? decimator.BaseAttackRate : melee.AttackRate;

        decimator.SpeedMultiplier = MathF.Min(decimator.SpeedMultiplier + decimator.AttackSpeedGrowth, decimator.MaxAttackSpeedMultiplier);

        var newRate = baseRate * decimator.SpeedMultiplier;
        if (MathHelper.CloseTo(melee.AttackRate, newRate))
            return;

        melee.AttackRate = newRate;
        DirtyField(weaponUid, melee, nameof(NivalisMeleeComponent.AttackRate));
    }

    private void ResetMomentum(EntityUid weaponUid, NivalisDecimatorComponent decimator, NivalisMeleeComponent melee)
    {
        if (MathHelper.CloseTo(decimator.SpeedMultiplier, 1f))
            return;

        decimator.SpeedMultiplier = 1f;

        var baseRate = decimator.BaseAttackRate > 0f ? decimator.BaseAttackRate : melee.AttackRate;
        if (MathHelper.CloseTo(melee.AttackRate, baseRate))
            return;

        melee.AttackRate = baseRate;
        DirtyField(weaponUid, melee, nameof(NivalisMeleeComponent.AttackRate));
    }
}
