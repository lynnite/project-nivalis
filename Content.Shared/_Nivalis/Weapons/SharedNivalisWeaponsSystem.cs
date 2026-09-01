using System.Numerics;
using Content.Shared.DoAfter;
using Content.Shared._Nivalis.Traits;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Movement.Systems;
using Content.Shared.Physics;
using Content.Shared.Popups;
using Content.Shared.Sprite;
using Content.Shared.Weapons.Ranged;
using Content.Shared.Weapons.Ranged.Systems;
using Content.Shared.Weapons.Hitscan.Components;
using Content.Shared.Weapons.Ranged.Components;
using Content.Shared.Weapons.Ranged.Events;
using Robust.Shared.Map;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Prototypes;
using Robust.Shared.Spawners;
using Robust.Shared.Utility;

namespace Content.Shared._Nivalis.Weapons;


public abstract partial class SharedNivalisWeaponsSystem : EntitySystem
{
    [Dependency] private SharedHandsSystem _hands = default!;
    [Dependency] private SharedDoAfterSystem _doAfter = default!;
    [Dependency] private MovementSpeedModifierSystem _movementSpeed = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private SharedTransformSystem _transformSystem = default!;
    [Dependency] private SharedScaleVisualsSystem _scaleVisuals = default!;

    public static readonly EntProtoId NivalisBulletTrail = "NivalisBulletTrail";

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

        SubscribeLocalEvent<NivalisGunComponent, AmmoShotEvent>(OnAmmoShot);
    }

    private void OnAmmoShot(Entity<NivalisGunComponent> gun, ref AmmoShotEvent args)
    {
        if (args.FiredProjectiles.Count == 0)
            return;

        var muzzleXform = Transform(gun);
        var muzzle = _transformSystem.ToMapCoordinates(muzzleXform.Coordinates);

        foreach (var projectile in args.FiredProjectiles)
        {
            if (!TryComp<PhysicsComponent>(projectile, out var physics) ||
                physics.LinearVelocity.LengthSquared() < 0.01f)
            {
                continue;
            }

            var trail = Spawn(NivalisBulletTrail, muzzle);
            var comp = EnsureComp<NivalisTrailComponent>(trail);
            comp.Target = projectile;
            comp.Muzzle = muzzle;
            Dirty(trail, comp);
        }
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<NivalisTrailComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            if (TerminatingOrDeleted(comp.Target))
            {
                if (!HasComp<TimedDespawnComponent>(uid))
                {
                    var despawn = AddComp<TimedDespawnComponent>(uid);
                    despawn.Lifetime = 1.0f;
                }
                continue;
            }

            var bulletPos = _transformSystem.GetMapCoordinates(comp.Target).Position;
            var offset = bulletPos - comp.Muzzle.Position;
            var distance = offset.Length();

            if (distance < 1f)
                continue;

            var direction = offset.Normalized();
            var midPoint = comp.Muzzle.Position + direction * (distance / 2f);

            _transformSystem.SetWorldPosition(uid, midPoint);
            _transformSystem.SetWorldRotation(uid, direction.ToAngle());
            _scaleVisuals.SetSpriteScale(uid, new Vector2(distance, 1f));
        }
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
        var delay = reload.ReloadDelay;
        if (TryComp<NivalisTraitComponent>(user, out var traits) && traits.ReloadDelayMult != 1f)
            delay *= traits.ReloadDelayMult;

        var doAfter = new DoAfterArgs(
            EntityManager,
            user,
            delay,
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
