using Content.Shared._Nivalis.Weapons;
using Content.Shared.Weapons.Ranged.Components;

namespace Content.Server._Nivalis.Weapons;

/// <summary>
///     Server half of the Nivalis scatter/aim system.
///
///     The scatter and aim spread is applied through <see cref="GunRefreshModifiersEvent"/>
///     in the shared <see cref="SharedNivalisScatterSystem"/>. This subclass just marks
///     scatter components initialized and resets the aim flag on startup.
/// </summary>
public sealed partial class NivalisScatterSystem : SharedNivalisScatterSystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<NivalisScatterGunComponent, ComponentStartup>(OnScatterStartup);
        SubscribeLocalEvent<NivalisAimComponent, ComponentStartup>(OnAimStartup);
    }

    private void OnScatterStartup(EntityUid uid, NivalisScatterGunComponent component, ComponentStartup args)
    {
        if (!component.ScatterInitialized)
        {
            component.ScatterInitialized = true;
            Dirty(uid, component);
        }
    }

    private void OnAimStartup(EntityUid uid, NivalisAimComponent component, ComponentStartup args)
    {
        component.Aiming = false;
        Dirty(uid, component);
    }
}
