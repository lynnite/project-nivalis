using System.Numerics;
using System.Reflection;
using Content.Server._Nivalis.Perks;
using Content.Server.Gravity;
using Content.Shared._Nivalis.Perks;
using Content.Shared.Gravity;
using Content.Shared.Throwing;
using Content.IntegrationTests.Fixtures;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;

namespace Content.IntegrationTests.Tests._Nivalis;

[TestFixture]
[TestOf(typeof(NivalisCrosslinkSystem))]
public sealed class NivalisCrosslinkTest : GameTest
{
    public override PoolSettings PoolSettings => new()
    {
        Connected = true,
        DummyTicker = false,
    };

    private static readonly EntProtoId KnifeProto = "NivalisCrosslinkKnife";
    private static readonly EntProtoId MobProto = "MobHuman";

    private static readonly MethodInfo OnAction =
        typeof(NivalisCrosslinkSystem).GetMethod("OnAction", BindingFlags.Instance | BindingFlags.NonPublic)!;

    [Test]
    public async Task KnivesWithinRangeAreLinkedAndOutOfRangeAreNot()
    {
        var map = await Pair.CreateTestMap();

        await Server.WaitPost(() =>
        {
            var mob = SEntMan.SpawnEntity(MobProto, map.GridCoords);

            SEntMan.EnsureComponent<NivalisPerkComponent>(mob);
            SEntMan.System<NivalisPerkSystem>().SetPerk(mob, "Crosslink");

            var cross = SEntMan.EnsureComponent<NivalisCrosslinkComponent>(mob);
            cross.Initialised = true;
            cross.Charge = 100f;

            var nearA = SEntMan.SpawnEntity(KnifeProto, new EntityCoordinates(map.Grid, new Vector2(2f, 0f)));
            var nearB = SEntMan.SpawnEntity(KnifeProto, new EntityCoordinates(map.Grid, new Vector2(4f, 0f)));
            var far = SEntMan.SpawnEntity(KnifeProto, new EntityCoordinates(map.Grid, new Vector2(2f, 100f)));

            foreach (var knife in new[] { nearA, nearB, far })
            {
                var kc = SEntMan.EnsureComponent<NivalisCrosslinkKnifeComponent>(knife);
                kc.OwnerPlayer = mob;
                cross.Knives.Add(knife);
                var land = new LandEvent(mob, false);
                SEntMan.EventBus.RaiseLocalEvent(knife, ref land);
            }
        });

        await Server.WaitRunTicks(2);

        await Server.WaitAssertion(() =>
        {
            var wires = 0;
            var query = SEntMan.EntityQueryEnumerator<NivalisCrosslinkWireComponent>();
            while (query.MoveNext(out _, out var wire))
            {
                wires++;
                Assert.That(wire.KnifeA, Is.Not.Null);
                Assert.That(wire.KnifeB, Is.Not.Null);
            }

            Assert.That(wires, Is.EqualTo(1));
        });
    }

    [Test]
    public async Task DeletingKnifeRemovesItsWires()
    {
        var map = await Pair.CreateTestMap();

        EntityUid nearB = default;

        await Server.WaitPost(() =>
        {
            var mob = SEntMan.SpawnEntity(MobProto, map.GridCoords);

            SEntMan.EnsureComponent<NivalisPerkComponent>(mob);
            SEntMan.System<NivalisPerkSystem>().SetPerk(mob, "Crosslink");

            var cross = SEntMan.EnsureComponent<NivalisCrosslinkComponent>(mob);
            cross.Initialised = true;
            cross.Charge = 100f;

            var nearA = SEntMan.SpawnEntity(KnifeProto, new EntityCoordinates(map.Grid, new Vector2(2f, 0f)));
            nearB = SEntMan.SpawnEntity(KnifeProto, new EntityCoordinates(map.Grid, new Vector2(6f, 0f)));

            foreach (var knife in new[] { nearA, nearB })
            {
                var kc = SEntMan.EnsureComponent<NivalisCrosslinkKnifeComponent>(knife);
                kc.OwnerPlayer = mob;
                cross.Knives.Add(knife);
                var land = new LandEvent(mob, false);
                SEntMan.EventBus.RaiseLocalEvent(knife, ref land);
            }
        });

        await Server.WaitRunTicks(2);

        await Server.WaitAssertion(() =>
        {
            var initialWires = 0;
            var initialQuery = SEntMan.EntityQueryEnumerator<NivalisCrosslinkWireComponent>();
            while (initialQuery.MoveNext(out _, out _))
                initialWires++;

            Assert.That(initialWires, Is.EqualTo(1), "Expected the two knives to be wired together before deleting one.");
        });

        await Server.WaitPost(() =>
        {
            SEntMan.DeleteEntity(nearB);
        });

        await Server.WaitRunTicks(2);

        await Server.WaitAssertion(() =>
        {
            var wires = 0;
            var query = SEntMan.EntityQueryEnumerator<NivalisCrosslinkWireComponent>();
            while (query.MoveNext(out _, out _))
                wires++;

            Assert.That(wires, Is.Zero, "Wires should be removed when an anchored knife is deleted.");
        });
    }

    [Test]
    public async Task ThrowPlantWireAndRecall()
    {
        var map = await Pair.CreateTestMap();

        await Server.WaitPost(() =>
        {
            var gravity = SEntMan.EnsureComponent<GravityComponent>(map.Grid);
            SEntMan.System<GravitySystem>().EnableGravity(map.Grid, gravity);
        });

        await Server.WaitRunTicks(2);

        var session = ServerSession;
        Assert.That(session?.AttachedEntity, Is.Not.Null);

        var player = session!.AttachedEntity!.Value;
        var playerPos = Vector2.Zero;

        await Server.WaitPost(() =>
        {
            SEntMan.EnsureComponent<NivalisPerkComponent>(player);
            SEntMan.System<NivalisPerkSystem>().SetPerk(player, "Crosslink");
            var cross = SEntMan.EnsureComponent<NivalisCrosslinkComponent>(player);
            cross.Initialised = true;
            cross.Charge = 100f;

            playerPos = SEntMan.System<SharedTransformSystem>().GetWorldPosition(player);
        });

        await Server.WaitRunTicks(2);

        var aim = new Vector2(0f, 1f);
        var firstTarget = new EntityCoordinates(map.Grid, playerPos + aim * 5f);

        await RaiseAction(session, NivalisCrosslinkAction.PlaceKnife, firstTarget, aim);
        await Server.WaitRunTicks(60);

        await Server.WaitAssertion(() =>
        {
            var cross = SEntMan.GetComponent<NivalisCrosslinkComponent>(player);
            Assert.That(cross.Knives, Has.Count.EqualTo(1), "Expected exactly one knife to be deployed.");

            var knife = cross.Knives[0];
            var knifePos = SEntMan.System<SharedTransformSystem>().GetWorldPosition(knife);
            var offset = knifePos - playerPos;

            Assert.That(offset.Y, Is.GreaterThan(1f), $"Knife did not travel along +Y (offset was {offset}).");
            Assert.That(MathF.Abs(offset.X), Is.LessThan(2f), $"Knife drifted on X (offset was {offset}).");

            var kc = SEntMan.GetComponent<NivalisCrosslinkKnifeComponent>(knife);
            Assert.That(kc.Planted, Is.True, "Knife did not plant after landing.");
        });

        var secondTarget = new EntityCoordinates(map.Grid, playerPos + new Vector2(6f, 5f));
        await RaiseAction(session, NivalisCrosslinkAction.PlaceKnife, secondTarget, new Vector2(1f, 0f));
        await Server.WaitRunTicks(60);

        await Server.WaitAssertion(() =>
        {
            var wires = 0;
            var query = SEntMan.EntityQueryEnumerator<NivalisCrosslinkWireComponent>();
            while (query.MoveNext(out _, out _))
                wires++;

            Assert.That(wires, Is.GreaterThanOrEqualTo(1), "Two nearby planted knives should be wired together.");
        });

        await RaiseAction(session, NivalisCrosslinkAction.RecallKnife,
            new EntityCoordinates(map.Grid, playerPos + aim * 5f), aim);
        await Server.WaitRunTicks(2);

        await Server.WaitAssertion(() =>
        {
            var cross = SEntMan.GetComponent<NivalisCrosslinkComponent>(player);
            Assert.That(cross.Knives, Has.Count.EqualTo(1), "Recalling should have removed one knife.");
            Assert.That(cross.Charge, Is.GreaterThan(60f), "Recalling a knife should refund charge.");
        });
    }

    [Test]
    public async Task WireSnaresEnemiesButNotOwner()
    {
        var map = await Pair.CreateTestMap();

        EntityUid victim = default;
        EntityUid owner = default;

        await Server.WaitPost(() =>
        {
            owner = SEntMan.SpawnEntity(MobProto, map.GridCoords);
            SEntMan.EnsureComponent<NivalisPerkComponent>(owner);
            SEntMan.System<NivalisPerkSystem>().SetPerk(owner, "Crosslink");
            var cross = SEntMan.EnsureComponent<NivalisCrosslinkComponent>(owner);
            cross.Initialised = true;
            cross.Charge = 100f;

            var a = SEntMan.SpawnEntity(KnifeProto, new EntityCoordinates(map.Grid, new Vector2(0f, 0f)));
            var b = SEntMan.SpawnEntity(KnifeProto, new EntityCoordinates(map.Grid, new Vector2(8f, 0f)));

            foreach (var knife in new[] { a, b })
            {
                var kc = SEntMan.EnsureComponent<NivalisCrosslinkKnifeComponent>(knife);
                kc.OwnerPlayer = owner;
                cross.Knives.Add(knife);
                var land = new LandEvent(owner, false);
                SEntMan.EventBus.RaiseLocalEvent(knife, ref land);
            }

            victim = SEntMan.SpawnEntity(MobProto, new EntityCoordinates(map.Grid, new Vector2(4f, 0f)));
        });

        await Server.WaitRunTicks(2);

        await Server.WaitAssertion(() =>
        {
            var status = SEntMan.System<Content.Shared.StatusEffectNew.StatusEffectsSystem>();
            Assert.That(status.HasStatusEffect(victim, Content.Shared.Stunnable.SharedStunSystem.StunId), Is.True,
                "Enemy standing on the wire should be stunned.");
            Assert.That(status.HasStatusEffect(owner, Content.Shared.Stunnable.SharedStunSystem.StunId), Is.False,
                "The wire owner should not be snared by their own wire.");
        });
    }

    [Test]
    public async Task WireSnareHasCooldownPerTarget()
    {
        var map = await Pair.CreateTestMap();

        EntityUid victim = default;
        EntityUid wire = default;

        await Server.WaitPost(() =>
        {
            var owner = SEntMan.SpawnEntity(MobProto, map.GridCoords);
            SEntMan.System<NivalisPerkSystem>().SetPerk(owner, "Crosslink");
            var cross = SEntMan.EnsureComponent<NivalisCrosslinkComponent>(owner);
            cross.Initialised = true;
            cross.Charge = 100f;

            var a = SEntMan.SpawnEntity(KnifeProto, new EntityCoordinates(map.Grid, new Vector2(0f, 0f)));
            var b = SEntMan.SpawnEntity(KnifeProto, new EntityCoordinates(map.Grid, new Vector2(8f, 0f)));

            foreach (var knife in new[] { a, b })
            {
                var kc = SEntMan.EnsureComponent<NivalisCrosslinkKnifeComponent>(knife);
                kc.OwnerPlayer = owner;
                cross.Knives.Add(knife);
                var land = new LandEvent(owner, false);
                SEntMan.EventBus.RaiseLocalEvent(knife, ref land);
            }

            victim = SEntMan.SpawnEntity(MobProto, new EntityCoordinates(map.Grid, new Vector2(4f, 0f)));
        });

        await Server.WaitRunTicks(2);

        await Server.WaitAssertion(() =>
        {
            var query = SEntMan.EntityQueryEnumerator<NivalisCrosslinkWireComponent>();
            while (query.MoveNext(out var wireUid, out _))
            {
                wire = wireUid;
            }

            var crosslink = SEntMan.System<NivalisCrosslinkSystem>();
            Assert.That(wire, Is.Not.EqualTo(EntityUid.Invalid), "Expected a wire to have been created.");
            Assert.That(crosslink.IsSnareOnCooldown(wire, victim), Is.True,
                "Victim should be on the wire's snare cooldown after being snared.");

            var status = SEntMan.System<Content.Shared.StatusEffectNew.StatusEffectsSystem>();
            Assert.That(status.HasStatusEffect(victim, Content.Shared.Stunnable.SharedStunSystem.StunId), Is.True,
                "Victim should have been snared.");
        });

        await Server.WaitRunTicks(150);

        await Server.WaitAssertion(() =>
        {
            var crosslink = SEntMan.System<NivalisCrosslinkSystem>();
            Assert.That(crosslink.IsSnareOnCooldown(wire, victim), Is.True,
                "Victim should remain on cooldown after the snare stun ends (10s cooldown > 3s snare).");
        });
    }

    [Test]
    public async Task RecallDrawsWireVfx()
    {
        var map = await Pair.CreateTestMap();

        await Server.WaitPost(() =>
        {
            var gravity = SEntMan.EnsureComponent<GravityComponent>(map.Grid);
            SEntMan.System<GravitySystem>().EnableGravity(map.Grid, gravity);
        });

        await Server.WaitRunTicks(2);

        var session = ServerSession;
        Assert.That(session?.AttachedEntity, Is.Not.Null);

        var player = session!.AttachedEntity!.Value;
        var playerPos = Vector2.Zero;

        await Server.WaitPost(() =>
        {
            SEntMan.System<NivalisPerkSystem>().SetPerk(player, "Crosslink");
            var cross = SEntMan.EnsureComponent<NivalisCrosslinkComponent>(player);
            cross.Initialised = true;
            cross.Charge = 100f;
            playerPos = SEntMan.System<SharedTransformSystem>().GetWorldPosition(player);
        });

        await Server.WaitRunTicks(2);

        var aim = new Vector2(0f, 1f);
        await RaiseAction(session, NivalisCrosslinkAction.PlaceKnife,
            new EntityCoordinates(map.Grid, playerPos + aim * 6f), aim);
        await Server.WaitRunTicks(60);

        // Recall and check the vfx entity appears.
        await RaiseAction(session, NivalisCrosslinkAction.RecallKnife,
            new EntityCoordinates(map.Grid, playerPos + aim * 6f), aim);
        await Server.WaitRunTicks(1);

        await Server.WaitAssertion(() =>
        {
            var vfxCount = 0;
            var query = SEntMan.EntityQueryEnumerator<NivalisCrosslinkRecallVfxComponent>();
            while (query.MoveNext(out _, out var vfx))
            {
                vfxCount++;
                Assert.That(vfx.SegmentCount, Is.GreaterThan(0));
            }

            Assert.That(vfxCount, Is.EqualTo(1), "Recalling a knife should start exactly one pull-in effect.");
        });

        await Server.WaitRunTicks(300);

        await Server.WaitAssertion(() =>
        {
            var vfxCount = 0;
            var query = SEntMan.EntityQueryEnumerator<NivalisCrosslinkRecallVfxComponent>();
            while (query.MoveNext(out _, out _))
                vfxCount++;

            Assert.That(vfxCount, Is.Zero, "Recall effect should have cleaned itself up.");

            var segmentCount = 0;
            var segQuery = SEntMan.EntityQueryEnumerator<MetaDataComponent>();
            while (segQuery.MoveNext(out var uid, out var meta))
            {
                if (meta.EntityPrototype?.ID == "NivalisCrosslinkRecallSegment")
                    segmentCount++;
            }

            Assert.That(segmentCount, Is.Zero, "Recall effect segments should have been deleted.");
        });
    }

    private async Task RaiseAction(ICommonSession session, NivalisCrosslinkAction action, EntityCoordinates target, Vector2 aim)
    {
        await Server.WaitPost(() =>
        {
            var msg = new NivalisCrosslinkActionMessage(action, SEntMan.GetNetCoordinates(target), aim);
            var sys = SEntMan.System<NivalisCrosslinkSystem>();
            OnAction.Invoke(sys, [msg, new EntitySessionEventArgs(session)]);
        });
    }
}
