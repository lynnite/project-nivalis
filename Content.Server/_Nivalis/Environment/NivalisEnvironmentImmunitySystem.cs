using Content.Server.Atmos.EntitySystems;
using Content.Server.Body.Components;
using Content.Shared._Nivalis.Environment;

namespace Content.Server._Nivalis.Environment;

public sealed partial class NivalisEnvironmentImmunitySystem : SharedNivalisEnvironmentImmunitySystem
{
    [Dependency] private BarotraumaSystem _barotrauma = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<NivalisEnvironmentImmunityComponent, ComponentInit>(OnComponentInit);
        SubscribeLocalEvent<NivalisEnvironmentImmunityComponent, ComponentStartup>(OnComponentStartup);
    }

    private void OnComponentInit(Entity<NivalisEnvironmentImmunityComponent> ent, ref ComponentInit args)
    {
        ApplyImmunities(ent);
    }

    private void OnComponentStartup(Entity<NivalisEnvironmentImmunityComponent> ent, ref ComponentStartup args)
    {
        ApplyImmunities(ent);
    }

    private void ApplyImmunities(Entity<NivalisEnvironmentImmunityComponent> ent)
    {
        RemComp<RespiratorComponent>(ent);

        _barotrauma.RefreshPressureImmunity(ent);
    }
}
