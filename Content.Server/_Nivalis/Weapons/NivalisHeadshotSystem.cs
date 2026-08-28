using Content.Shared._Nivalis.Weapons;
using Content.Shared.Damage;
using Content.Shared.Projectiles;
using Content.Shared.Weapons.Ranged.Components;

namespace Content.Server._Nivalis.Weapons;

public sealed partial class NivalisHeadshotSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ProjectileComponent, BeforeProjectileHitEvent>(OnBeforeProjectileHit);
    }

    private void OnBeforeProjectileHit(Entity<ProjectileComponent> projectile, ref BeforeProjectileHitEvent args)
    {
        if (args.Target == EntityUid.Invalid || Deleted(args.Target))
            return;

        if (!TryComp<NivalisGunComponent>(projectile.Comp.Weapon, out var gun))
            return;

        var isHeadshot = TryComp<TargetedProjectileComponent>(projectile, out var targeted)
            && targeted.Target == args.Target;

        DamageSpecifier? damage = null;
        if (isHeadshot && gun.HeadshotDamage is { } headshot)
            damage = headshot;
        else if (!isHeadshot && gun.LimbDamage is { } limb)
            damage = limb;

        if (damage != null)
            args.Damage = damage;
    }
}
