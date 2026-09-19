using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Content.Server._Nivalis.Weapons;
using Content.Shared._Nivalis.Health;
using Content.Shared._Nivalis.Melee;
using Content.Shared._Nivalis.Weapons;
using Content.Shared.Damage;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Systems;
using Content.Shared.FixedPoint;
using Content.Shared.Inventory;
using Content.IntegrationTests.Fixtures;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Maths;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.IntegrationTests.Tests._Nivalis;

[TestFixture]
[TestOf(typeof(NivalisDecimatorSystem))]
public sealed class NivalisDecimatorTest : GameTest
{
    private static readonly EntProtoId WeaponProto = "NivalisDecimator";
    private static readonly EntProtoId MobProto = "MobHuman";
    private static readonly EntProtoId HelmetProto = "ClothingHeadHelmetSwatSyndicate";

    [Test]
    public async Task HeavyHitDetonationAndDecapitationSpawnBloodAndMark()
    {
        var map = await Pair.CreateTestMap();

        EntityUid weapon = default;
        EntityUid victim = default;
        EntityUid attacker = default;

        await Server.WaitPost(() =>
        {
            attacker = SEntMan.SpawnEntity(MobProto, map.GridCoords);
            weapon = SEntMan.SpawnEntity(WeaponProto, map.GridCoords);

            victim = SEntMan.SpawnEntity(MobProto, new EntityCoordinates(map.Grid, new Vector2(0.5f, 0f)));
        });

        await Server.WaitRunTicks(2);

        var attackerDamageBefore = FixedPoint2.Zero;
        await Server.WaitPost(() => attackerDamageBefore = GetTotalDamage(attacker));

        await Server.WaitAssertion(() =>
        {
            var dmg = new DamageSpecifier();
            var ev = new NivalisMeleeHitEvent(new List<EntityUid> { victim }, attacker, weapon, dmg, Vector2.UnitX, heavy: true);
            SEntMan.EventBus.RaiseLocalEvent(weapon, ev, broadcast: true);
        });

        await Server.WaitRunTicks(2);

        await Server.WaitAssertion(() =>
        {
            Assert.That(SEntMan.HasComponent<NivalisDecimatorMarkComponent>(victim), Is.True,
                "A mob caught in the heavy blast should be marked for decapitation.");

            Assert.That(GetTotalDamage(attacker), Is.EqualTo(attackerDamageBefore),
                "The user should not take damage from their own Decimator detonation.");
        });
    }

    private FixedPoint2 GetTotalDamage(EntityUid uid)
    {
        var damageable = SEntMan.GetComponent<DamageableComponent>(uid);
        return SEntMan.System<DamageableSystem>().GetPositiveDamage((uid, damageable)).GetTotal();
    }

    [Test]
    public async Task KillingAMarkedMobDecapitatesItDeletesHeadwearAndSpawnsBlood()
    {
        var map = await Pair.CreateTestMap();

        EntityUid victim = default;
        EntityUid attacker = default;
        EntityUid helmet = default;

        await Server.WaitPost(() =>
        {
            attacker = SEntMan.SpawnEntity(MobProto, map.GridCoords);
            victim = SEntMan.SpawnEntity(MobProto, new EntityCoordinates(map.Grid, new Vector2(0.5f, 0f)));

            var inv = SEntMan.System<InventorySystem>();
            helmet = SEntMan.SpawnEntity(HelmetProto, map.GridCoords);
            Assert.That(inv.TryEquip(victim, helmet, "head", silent: true), Is.True, "Helmet failed to equip.");

            var mark = SEntMan.EnsureComponent<NivalisDecimatorMarkComponent>(victim);
            mark.ExpiresAt = Server.ResolveDependency<IGameTiming>().CurTime + TimeSpan.FromSeconds(1);
        });

        await Server.WaitRunTicks(1);

        await Server.WaitPost(() =>
        {
            var dmg = new DamageSpecifier();
            dmg.DamageDict["Asphyxiation"] = 300;
            SEntMan.System<Content.Shared.Damage.Systems.DamageableSystem>().TryChangeDamage(victim, dmg, origin: attacker);
        });

        await Server.WaitRunTicks(2);

        await Server.WaitAssertion(() =>
        {
            Assert.That(SEntMan.HasComponent<NivalisDecapitatedComponent>(victim), Is.True,
                "A marked mob that dies should be decapitated.");

            Assert.That(SEntMan.Deleted(helmet) || !SEntMan.EntityExists(helmet), Is.True,
                "The victim's helmet should be deleted along with its head.");
        });

        var bloodSpawned = false;
        await Server.WaitPost(() =>
        {
            var query = SEntMan.EntityQueryEnumerator<NivalisBloodSplurtComponent>();
            while (query.MoveNext(out _, out _))
                bloodSpawned = true;
        });

        Assert.That(bloodSpawned, Is.True, "Decapitation should spawn a blood spray effect.");
    }

    [Test]
    public async Task DecapitationLeavesBloodDecalsOnTheGround()
    {
        var map = await Pair.CreateTestMap();

        EntityUid victim = default;
        EntityUid attacker = default;

        await Server.WaitPost(() =>
        {
            var mapSys = SEntMan.System<SharedMapSystem>();
            var plating = Server.ResolveDependency<ITileDefinitionManager>()["Plating"].TileId;
            for (var x = -5; x <= 5; x++)
            {
                for (var y = -5; y <= 5; y++)
                {
                    var coords = map.GridCoords.Offset(new Vector2(x, y));
                    mapSys.SetTile(map.Grid.Owner, map.Grid.Comp, coords, new Tile(plating));
                }
            }

            attacker = SEntMan.SpawnEntity(MobProto, map.GridCoords);
            victim = SEntMan.SpawnEntity(MobProto, map.GridCoords);

            var mark = SEntMan.EnsureComponent<NivalisDecimatorMarkComponent>(victim);
            mark.ExpiresAt = Server.ResolveDependency<IGameTiming>().CurTime + TimeSpan.FromSeconds(1);
        });

        await Server.WaitRunTicks(1);

        await Server.WaitPost(() =>
        {
            var dmg = new DamageSpecifier();
            dmg.DamageDict["Asphyxiation"] = 300;
            SEntMan.System<Content.Shared.Damage.Systems.DamageableSystem>().TryChangeDamage(victim, dmg, origin: attacker);
        });

        await Server.WaitRunTicks(120);

        await Server.WaitAssertion(() =>
        {
            var xform = SEntMan.GetComponent<TransformComponent>(victim);
            var gridUid = xform.GridUid!.Value;

            var local = xform.Coordinates.Position;
            var bounds = new Box2(local - new Vector2(6f), local + new Vector2(6f));
            var decals = SEntMan.System<Content.Server.Decals.DecalSystem>()
                .GetDecalsIntersecting(gridUid, bounds)
                .Where(entry => entry.Decal.Id.StartsWith("NivalisBloodFloor"))
                .ToList();

            Assert.That(decals, Is.Not.Empty,
                "Decapitation should leave blood decals on the ground, but none were found. " +
                $"(decapitated={SEntMan.HasComponent<NivalisDecapitatedComponent>(victim)}, " +
                $"local={local})");

            var mapSys = SEntMan.System<SharedMapSystem>();
            var corpseTile = mapSys.TileIndicesFor(gridUid, SEntMan.GetComponent<MapGridComponent>(gridUid), xform.Coordinates);

            var offsets = decals
                .Select(d => mapSys.TileIndicesFor(gridUid, SEntMan.GetComponent<MapGridComponent>(gridUid),
                    new EntityCoordinates(gridUid, d.Decal.Coordinates)))
                .Select(t => t - corpseTile)
                .ToList();

            Assert.That(offsets.Select(o => o.Y), Is.All.EqualTo(0),
                "Blood decals should stay on the same tile row as the corpse.");

            foreach (var offset in offsets)
            {
                Assert.That(-offset.X, Is.InRange(1, 3),
                    $"Blood decals should land 1-3 tiles to the west (left) of the corpse, but got offset {offset}.");
            }
        });
    }
}


