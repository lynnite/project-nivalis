using Content.Shared._Nivalis.Perks;
using Content.Shared._Nivalis.Traits;
using Content.Shared.Hands.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Weapons.Ranged.Components;
using Content.Shared.Weapons.Ranged.Events;
using Content.Shared.Weapons.Ranged.Systems;
using Robust.Shared.Maths;

namespace Content.Shared._Nivalis.Weapons;

public abstract partial class SharedNivalisScatterSystem : EntitySystem
{
    [Dependency] private SharedGunSystem _gun = default!;
    [Dependency] private SharedHandsSystem _hands = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<GunComponent, GunRefreshModifiersEvent>(OnGunRefresh);
    }

    private void OnGunRefresh(Entity<GunComponent> ent, ref GunRefreshModifiersEvent args)
    {
        var uid = ent.Owner;

        var holder = FindHolder(uid);
        NivalisTraitComponent? traits = null;
        NivalisPerkComponent? perks = null;
        if (holder != null)
        {
            TryComp<NivalisTraitComponent>(holder.Value, out traits);
            TryComp<NivalisPerkComponent>(holder.Value, out perks);
        }

        if (traits != null)
        {
            if (TryComp<NivalisGunComponent>(uid, out var nivalisGun))
            {
                if (nivalisGun.Fanning)
                {
                    args.FireRate *= traits.FanningFireRateMult * (perks?.FanningFireRateMult ?? 1f);
                }
                else if (ent.Comp.SelectedMode == SelectiveFire.SemiAuto)
                {
                    args.FireRate *= traits.SemiAutoFireRateMult * (perks?.SemiAutoFireRateMult ?? 1f);
                }
            }
            else if (ent.Comp.SelectedMode == SelectiveFire.SemiAuto)
            {
                args.FireRate *= traits.SemiAutoFireRateMult * (perks?.SemiAutoFireRateMult ?? 1f);
            }

            var recoilMult = traits.RecoilMult * (perks?.RecoilMult ?? 1f);
            if (!MathHelper.CloseTo(recoilMult, 1f))
            {
                args.CameraRecoilScalar *= recoilMult;
                if (IsAiming(uid))
                    args.AngleIncrease *= recoilMult;
            }
        }
        else if (perks != null)
        {
            if (TryComp<NivalisGunComponent>(uid, out var nivalisGun2))
            {
                if (nivalisGun2.Fanning)
                    args.FireRate *= perks.FanningFireRateMult;
                else if (ent.Comp.SelectedMode == SelectiveFire.SemiAuto)
                    args.FireRate *= perks.SemiAutoFireRateMult;
            }
            else if (ent.Comp.SelectedMode == SelectiveFire.SemiAuto)
            {
                args.FireRate *= perks.SemiAutoFireRateMult;
            }

            if (!MathHelper.CloseTo(perks.RecoilMult, 1f))
            {
                args.CameraRecoilScalar *= perks.RecoilMult;
                if (IsAiming(uid))
                    args.AngleIncrease *= perks.RecoilMult;
            }
        }

        if (TryComp<NivalisScatterGunComponent>(uid, out var scatter) && scatter.DegreesScatter > 0)
        {
            args.MaxAngle = Math.Max(args.MaxAngle, Angle.FromDegrees(scatter.DegreesScatter));
        }

        var aiming = IsAiming(uid);

        if (aiming && TryComp<NivalisAimedScatterComponent>(uid, out var aimed))
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

        if (!aiming)
        {
            var hipFireMult = (traits?.HipFireSpreadMult ?? 1f) * (perks?.HipFireSpreadMult ?? 1f);
            if (!MathHelper.CloseTo(hipFireMult, 1f))
                args.MaxAngle = Angle.FromDegrees(args.MaxAngle.Degrees * hipFireMult);
        }
    }

    private EntityUid? FindHolder(EntityUid gun)
    {
        var query = EntityQueryEnumerator<HandsComponent>();
        while (query.MoveNext(out var owner, out var hands))
        {
            foreach (var handName in hands.SortedHands)
            {
                if (_hands.TryGetHeldItem((owner, hands), handName, out var held) && held == gun)
                    return owner;
            }
        }

        return null;
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
