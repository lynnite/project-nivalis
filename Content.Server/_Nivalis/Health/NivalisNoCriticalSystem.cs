using Content.Shared._Nivalis.Health;
using Content.Shared.Mobs;
using Content.Shared.Standing;
using Robust.Shared.Timing;

namespace Content.Server._Nivalis.Health;

public sealed partial class NivalisNoCriticalSystem : SharedNivalisNoCriticalSystem
{
    public const float BodyDeletionDelay = 2f;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<NivalisNoCriticalComponent, MobStateChangedEvent>(OnMobStateChanged);
        SubscribeLocalEvent<FellDownThrowAttemptEvent>(OnFellDownThrow);
    }

    private void OnMobStateChanged(Entity<NivalisNoCriticalComponent> ent, ref MobStateChangedEvent args)
    {
        if (args.NewMobState != MobState.Dead)
            return;

        var uid = ent.Owner;
        Timer.Spawn(TimeSpan.FromSeconds(BodyDeletionDelay), () =>
        {
            if (Exists(uid) && !Deleted(uid))
                QueueDel(uid);
        });
    }

    private void OnFellDownThrow(ref FellDownThrowAttemptEvent args)
    {
        if (args.Thrower.IsValid() && HasComp<NivalisNoCriticalComponent>(args.Thrower))
            args.Cancelled = true;
    }
}
