using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;

namespace Content.Shared._Nivalis.Health;

public abstract partial class SharedNivalisNoCriticalSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<NivalisNoCriticalComponent, UpdateMobStateEvent>(OnUpdateMobState);
    }

    private void OnUpdateMobState(EntityUid uid, NivalisNoCriticalComponent component, ref UpdateMobStateEvent args)
    {
        if (args.State != MobState.Critical)
            return;

        args.State = MobState.Dead;
    }
}

