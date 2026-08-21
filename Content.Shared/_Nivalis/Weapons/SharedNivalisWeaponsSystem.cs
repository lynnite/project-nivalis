using Content.Shared.DoAfter;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Movement.Systems;
using Content.Shared.Weapons.Ranged;
using Content.Shared.Weapons.Hitscan.Components;
using Content.Shared.Weapons.Ranged.Components;
using Content.Shared.Weapons.Ranged.Events;
using Robust.Shared.Prototypes;

namespace Content.Shared._Nivalis.Weapons;

public abstract partial class SharedNivalisWeaponsSystem : EntitySystem
{
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly MovementSpeedModifierSystem _movementSpeed = default!;

    public override void Initialize()
    {
        base.Initialize();

        InitializeWeapons();

        SubmitReloadBindings();
    }

    protected virtual void InitializeWeapons()
    {
        SubscribeLocalEvent<NivalisPoolAmmoProviderComponent, TakeAmmoEvent>(OnTakeAmmo);
        SubscribeLocalEvent<NivalisPoolAmmoProviderComponent, GetAmmoCountEvent>(OnGetAmmoCount);

        SubscribeLocalEvent<NivalisGunComponent, MapInitEvent>(OnGunMapInit);

        SubscribeLocalEvent<NivalisReloadComponent, NivalisReloadDoAfterEvent>(OnReloadDoAfter);
        SubscribeLocalEvent<NivalisAmmoPoolComponent, RefreshMovementSpeedModifiersEvent>(OnUserReloadMoveSpeed);
    }

    protected virtual void SubmitReloadBindings()
    {
        SubscribeAllEvent<NivalisReloadEvent>(OnReloadInput);
    }

    private void OnGunMapInit(Entity<NivalisGunComponent> ent, ref MapInitEvent args)
    {
        if (ent.Comp.MagazineCount >= 0)
            return;

        ent.Comp.MagazineCount = ent.Comp.MaxAmmo;
        Dirty(ent);
    }

    private void OnTakeAmmo(Entity<NivalisPoolAmmoProviderComponent> provider, ref TakeAmmoEvent args)
    {
        if (!TryComp<NivalisGunComponent>(provider, out var gun))
            return;

        var perShot = 1;
        if (TryComp<NivalisScatterGunComponent>(provider, out var scatter))
            perShot = Math.Max(1, scatter.ProjectilesPerShot);

        var toFire = Math.Min(gun.MagazineCount, args.Shots * perShot);
        for (var i = 0; i < toFire; i++)
        {
            gun.MagazineCount--;

            var ammoEnt = Spawn(gun.Projectile, args.Coordinates);
            args.Ammo.Add((ammoEnt, EnsureShootable(ammoEnt)));
        }

        if (toFire > 0)
            Dirty(provider, gun);
    }

    private void OnGetAmmoCount(Entity<NivalisPoolAmmoProviderComponent> provider, ref GetAmmoCountEvent args)
    {
        if (!TryComp<NivalisGunComponent>(provider, out var gun))
            return;

        args.Capacity = gun.MaxAmmo;
        args.Count = gun.MagazineCount;
    }

    private void OnReloadInput(NivalisReloadEvent msg, EntitySessionEventArgs args)
    {
        if (!msg.Active)
            return;

        var user = args.SenderSession.AttachedEntity;
        if (user == null)
            return;

        var held = _hands.GetActiveItem((user.Value, null));
        if (held == null || !TryComp<NivalisGunComponent>(held, out _))
            return;

        TryReload((EntityUid) held, user.Value);
    }

    public bool TryReload(EntityUid gun, EntityUid user)
    {
        if (!TryComp<NivalisGunComponent>(gun, out var gunComp))
            return false;

        if (gunComp.MagazineCount >= gunComp.MaxAmmo)
            return false;

        if (TryComp<NivalisReloadComponent>(gun, out var reload))
        {
            var doAfter = new DoAfterArgs(
                EntityManager,
                user,
                reload.ReloadDelay,
                new NivalisReloadDoAfterEvent(),
                eventTarget: gun,
                used: gun)
            {
                BreakOnMove = reload.BreakOnMove,
                BreakOnWeightlessMove = reload.BreakOnMove,
                NeedHand = true,
                BreakOnHandChange = true,
                BreakOnDropItem = true,
            };

            if (!_doAfter.TryStartDoAfter(doAfter))
                return false;

            SetReloading(gun, user, true);
            return true;
        }

        return ReloadGun(gun, user);
    }

    private void OnReloadDoAfter(Entity<NivalisReloadComponent> gun, ref NivalisReloadDoAfterEvent args)
    {
        var user = args.User;

        SetReloading(gun.Owner, user, false);

        if (args.Cancelled || args.Handled)
            return;

        args.Handled = true;
        ReloadGun(gun, user);
    }

    private void SetReloading(EntityUid gun, EntityUid user, bool reloading)
    {
        if (TryComp<NivalisGunComponent>(gun, out var gunComp))
        {
            gunComp.Reloading = reloading;
            Dirty(gun, gunComp);
        }

        if (user.IsValid())
            _movementSpeed.RefreshMovementSpeedModifiers(user);
    }

    private void OnUserReloadMoveSpeed(Entity<NivalisAmmoPoolComponent> user, ref RefreshMovementSpeedModifiersEvent args)
    {
        var held = _hands.GetActiveItem((user.Owner, null));
        if (held == null ||
            !TryComp<NivalisGunComponent>(held, out var gun) ||
            !gun.Reloading)
        {
            return;
        }

        if (TryComp<NivalisReloadComponent>(held, out var reload))
            args.ModifySpeed(reload.WalkSpeedMultiplier, reload.SprintSpeedMultiplier);
    }

    public bool ReloadGun(EntityUid gun, EntityUid shooter)
    {
        if (!TryComp<NivalisGunComponent>(gun, out var gunComp))
            return false;

        if (!TryComp<NivalisAmmoPoolComponent>(shooter, out var pool))
            return false;

        var want = gunComp.MaxAmmo - gunComp.MagazineCount;
        if (want <= 0)
            return false;

        var available = pool.GetAmmo(gunComp.AmmoType);
        if (available <= 0)
            return false;

        var transfer = Math.Min(want, available);
        gunComp.MagazineCount += transfer;
        pool.SetAmmo(gunComp.AmmoType, available - transfer);

        Dirty(gun, gunComp);
        Dirty(shooter, pool);
        return true;
    }

    protected IShootable EnsureShootable(EntityUid uid)
    {
        if (TryComp<CartridgeAmmoComponent>(uid, out var cartridge))
            return cartridge;

        if (TryComp<HitscanAmmoComponent>(uid, out var hitscanAmmo))
            return hitscanAmmo;

        return EnsureComp<AmmoComponent>(uid);
    }
}
