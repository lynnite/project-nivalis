using Content.Shared.DoAfter;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Movement.Systems;
using Content.Shared.Popups;
using Content.Shared.Weapons.Ranged;
using Content.Shared.Weapons.Ranged.Systems;
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
    [Dependency] private readonly SharedPopupSystem _popup = default!;

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
        SubscribeLocalEvent<NivalisGunComponent, AttemptShootEvent>(OnAttemptShoot);
    }

    private void OnAttemptShoot(Entity<NivalisGunComponent> gun, ref AttemptShootEvent args)
    {
        if (gun.Comp.Reloading)
            args.Cancelled = true;
    }

    protected virtual void SubmitReloadBindings()
    {
        SubscribeAllEvent<NivalisReloadEvent>(OnReloadInput);
        SubscribeAllEvent<NivalisUnloadEvent>(OnUnloadInput);
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


        var toFire = Math.Min(gun.MagazineCount, args.Shots);
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

    private void OnUnloadInput(NivalisUnloadEvent msg, EntitySessionEventArgs args)
    {
        var user = args.SenderSession.AttachedEntity;
        if (user == null)
            return;

        var held = _hands.GetActiveItem((user.Value, null));
        if (held == null || !TryComp<NivalisGunComponent>(held, out _))
            return;

        UnloadGunIntoPool((EntityUid) held, user.Value);
    }

    public bool UnloadGunIntoPool(EntityUid gun, EntityUid user)
    {
        if (!TryComp<NivalisGunComponent>(gun, out var gunComp))
            return false;

        if (!TryComp<NivalisAmmoPoolComponent>(user, out var pool))
            return false;

        if (gunComp.MagazineCount <= 0)
            return false;

        var collected = gunComp.MagazineCount;
        gunComp.MagazineCount = 0;

        var current = pool.GetAmmo(gunComp.AmmoType);
        pool.SetAmmo(gunComp.AmmoType, current + collected);

        Dirty(gun, gunComp);
        Dirty(user, pool);

        _popup.PopupEntity(Loc.GetString("nivalis-ammo-unload",
            ("amount", collected), ("type", Loc.GetString($"nivalis-ammo-{gunComp.AmmoType.ToString().ToLowerInvariant()}"))),
            user, user, PopupType.Medium);

        return true;
    }

    public bool TryReload(EntityUid gun, EntityUid user)
    {
        if (!TryComp<NivalisGunComponent>(gun, out var gunComp))
            return false;

        if (gunComp.MagazineCount >= gunComp.MaxAmmo)
            return false;

        if (!TryComp<NivalisAmmoPoolComponent>(user, out var pool) ||
            pool.GetAmmo(gunComp.AmmoType) <= 0)
        {
            return false;
        }

        if (TryComp<NivalisReloadComponent>(gun, out var reload))
            return StartReloadDoAfter(gun, user, reload);

        return ReloadGun(gun, user);
    }

    private bool StartReloadDoAfter(EntityUid gun, EntityUid user, NivalisReloadComponent reload)
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

    private void OnReloadDoAfter(Entity<NivalisReloadComponent> gun, ref NivalisReloadDoAfterEvent args)
    {
        var user = args.User;

        SetReloading(gun.Owner, user, false);

        if (args.Cancelled || args.Handled)
            return;

        args.Handled = true;
        if (!ReloadGun(gun, user))
            return;

        if (TryComp<NivalisGunComponent>(gun.Owner, out var gunComp) &&
            gunComp.MagazineCount < gunComp.MaxAmmo &&
            TryComp<NivalisAmmoPoolComponent>(user, out var pool) &&
            pool.GetAmmo(gunComp.AmmoType) > 0)
        {
            StartReloadDoAfter(gun.Owner, user, gun.Comp);
        }
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

        if (gunComp.ReloadAmount > 0)
            want = Math.Min(want, gunComp.ReloadAmount);

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
