using Content.Shared._Nivalis.Melee;
using Content.Shared.Body.Components;
using Content.Shared.Mobs.Components;
using Robust.Shared.Timing;

namespace Content.Shared._Nivalis.Status;

public sealed partial class NivalisBleedSuppressionSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;

    private EntityQuery<MobStateComponent> _mobStateQuery = default!;
    private EntityQuery<BloodstreamComponent> _bloodQuery = default!;

    public override void Initialize()
    {
        base.Initialize();

        _mobStateQuery = GetEntityQuery<MobStateComponent>();
        _bloodQuery = GetEntityQuery<BloodstreamComponent>();

        SubscribeLocalEvent<NivalisMeleeHitEvent>(OnMeleeHit);
    }

    private void OnMeleeHit(NivalisMeleeHitEvent args)
    {
        foreach (var target in args.HitEntities)
        {
            if (!_mobStateQuery.HasComp(target))
                continue;

            if (_bloodQuery.HasComp(target))
                SuppressNormalBleed(target);
        }
    }

    private void SuppressNormalBleed(EntityUid target)
    {
        if (!_bloodQuery.TryGetComponent(target, out var blood))
            return;

        var comp = EnsureComp<NivalisNoBleedComponent>(target);
        comp.OriginalBleedModifiers = blood.DamageBleedModifiers;
        blood.DamageBleedModifiers = "NivalisNoBleed";
        comp.RemoveAt = _timing.CurTime + TimeSpan.FromSeconds(0.6);
        Dirty(target, blood);
        Dirty(target, comp);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<NivalisNoBleedComponent>();
        while (query.MoveNext(out var uid, out var noBleed))
        {
            if (_timing.CurTime < noBleed.RemoveAt)
                continue;

            if (_bloodQuery.TryGetComponent(uid, out var blood))
            {
                blood.DamageBleedModifiers = noBleed.OriginalBleedModifiers;
                Dirty(uid, blood);
            }

            RemComp<NivalisNoBleedComponent>(uid);
        }
    }
}
