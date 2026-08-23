using Content.Shared.Weapons.Ranged.Components;
using Content.Shared.Weapons.Ranged.Events;
using Content.Shared.Weapons.Ranged.Systems;
using Robust.Shared.Maths;

namespace Content.Shared._Nivalis.Weapons;

public abstract partial class SharedNivalisScatterSystem : EntitySystem
{
    [Dependency] private SharedGunSystem _gun = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<GunComponent, GunRefreshModifiersEvent>(OnGunRefresh);
    }

    private void OnGunRefresh(Entity<GunComponent> ent, ref GunRefreshModifiersEvent args)
    {
        var uid = ent.Owner;

        if (TryComp<NivalisScatterGunComponent>(uid, out var scatter) && scatter.DegreesScatter > 0)
        {
            args.MaxAngle = Math.Max(args.MaxAngle, Angle.FromDegrees(scatter.DegreesScatter));
        }

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

    public bool IsAiming(EntityUid gun)
    {
        return TryComp<NivalisAimComponent>(gun, out var aim) && aim.Aiming;
    }

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
