using Content.Shared.Weapons.Ranged.Components;
using Content.Shared.Weapons.Ranged.Events;
using Content.Shared.Weapons.Ranged.Systems;
using Robust.Shared.Maths;

namespace Content.Shared._Nivalis.Weapons;

/// <summary>
///     Shared logic for Nivalis scatter guns and aiming.
///
///     Scatter/aim is applied through <see cref="GunRefreshModifiersEvent"/>, the sanctioned
///     way for other systems to adjust a gun's spread. The <see cref="NivalisAimComponent.Aiming"/>
///     flag is networked and driven by the right-mouse aim key; while aiming the tighter
///     <see cref="NivalisAimedScatterComponent"/> cone is used.
/// </summary>
public abstract partial class SharedNivalisScatterSystem : EntitySystem
{
    [Dependency] private readonly SharedGunSystem _gun = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<GunComponent, GunRefreshModifiersEvent>(OnGunRefresh);
    }

    private void OnGunRefresh(Entity<GunComponent> ent, ref GunRefreshModifiersEvent args)
    {
        var uid = ent.Owner;

        // Scatter gun static cone.
        if (TryComp<NivalisScatterGunComponent>(uid, out var scatter) && scatter.DegreesScatter > 0)
        {
            args.MaxAngle = Math.Max(args.MaxAngle, Angle.FromDegrees(scatter.DegreesScatter));
        }

        // Aiming / directional cone.
        if (IsAiming(uid) && TryComp<NivalisAimedScatterComponent>(uid, out var aimed))
        {
            var maxDeg = Math.Max(aimed.MaxLeft, aimed.MaxRight);
            args.MaxAngle = maxDeg == 0 ? args.MaxAngle : Math.Min(args.MaxAngle, Angle.FromDegrees(maxDeg * 2));
        }
        else if (TryComp<NivalisScatterDirectionComponent>(uid, out var dir))
        {
            var maxDeg = Math.Max(dir.MaxLeft, dir.MaxRight);
            if (maxDeg > 0)
                args.MaxAngle = Math.Max(args.MaxAngle, Angle.FromDegrees(maxDeg * 2));
        }
    }

    /// <summary>
    ///     Whether the gun is currently marked as being aimed.
    /// </summary>
    public bool IsAiming(EntityUid gun)
    {
        return TryComp<NivalisAimComponent>(gun, out var aim) && aim.Aiming;
    }

    /// <summary>
    ///     Updates the gun's aiming flag and re-refreshes the spread modifier so the
    ///     tightened cone is applied.
    /// </summary>
    public void SetAiming(EntityUid gun, bool aiming)
    {
        if (!TryComp<NivalisAimComponent>(gun, out var aim) || aim.Aiming == aiming)
            return;

        aim.Aiming = aiming;
        Dirty(gun, aim);

        if (TryComp<GunComponent>(gun, out var gunComp))
            _gun.RefreshModifiers((gun, gunComp));
    }
}
