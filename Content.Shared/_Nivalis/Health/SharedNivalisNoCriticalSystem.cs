using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;

namespace Content.Shared._Nivalis.Health;

/// <summary>
///     Makes entities with <see cref="NivalisNoCriticalComponent"/> skip the critical mobstate,
///     jumping straight from alive to dead once they pass their death threshold.
/// </summary>
public abstract partial class SharedNivalisNoCriticalSystem : EntitySystem
{
    [Dependency] private readonly MobThresholdSystem _thresholds = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<NivalisNoCriticalComponent, UpdateMobStateEvent>(OnUpdateMobState);
    }

    private void OnUpdateMobState(EntityUid uid, NivalisNoCriticalComponent component, ref UpdateMobStateEvent args)
    {
        if (args.State != MobState.Critical)
            return;

        // Redirect critical straight to death.
        args.State = MobState.Dead;

        // Cut the critical threshold out of the equation so nothing else tries to push
        // back up into critical.
        if (TryComp<MobThresholdsComponent>(uid, out var thresholds)
            && _thresholds.TryGetThresholdForState(uid, MobState.Dead, out _, thresholds))
        {
            // Only remove the critical threshold if a death threshold exists.
            _thresholds.SetMobStateThreshold(uid, -1f, MobState.Critical, thresholds);
        }
    }
}

