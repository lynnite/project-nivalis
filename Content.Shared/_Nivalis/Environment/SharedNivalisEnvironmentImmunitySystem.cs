using Content.Shared._Nivalis.Environment;
using Content.Shared.Atmos;
namespace Content.Shared._Nivalis.Environment;

public abstract partial class SharedNivalisEnvironmentImmunitySystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<NivalisEnvironmentImmunityComponent, RefreshPressureImmunityEvent>(OnRefreshPressureImmunity);
    }

    private void OnRefreshPressureImmunity(Entity<NivalisEnvironmentImmunityComponent> ent, ref RefreshPressureImmunityEvent args)
    {
        args.IsImmune = true;
    }
}

