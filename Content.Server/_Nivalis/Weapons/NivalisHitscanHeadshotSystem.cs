using Content.Shared._Nivalis.Weapons;
using Content.Shared.Damage;
using Content.Shared.Weapons.Hitscan.Components;
using Content.Shared.Weapons.Hitscan.Events;
using Content.Shared.Weapons.Hitscan.Systems;

namespace Content.Server._Nivalis.Weapons;

public sealed partial class NivalisHitscanHeadshotSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<NivalisHitscanHeadshotComponent, HitscanRaycastFiredEvent>(OnHitscanHit,
            before: new[] { typeof(HitscanBasicDamageSystem) });
    }

    private void OnHitscanHit(Entity<NivalisHitscanHeadshotComponent> ent, ref HitscanRaycastFiredEvent args)
    {
        if (!TryComp<HitscanBasicDamageComponent>(ent, out var damageComp))
            return;

        if (args.Data.HitEntity is not { } target)
            return;

        var isHeadshot = args.Data.Target == target;

        if (!TryComp<NivalisGunComponent>(args.Data.Gun, out var gun))
            return;

        var damage = isHeadshot ? gun.HeadshotDamage : gun.LimbDamage;
        if (damage == null)
            return;

        damageComp.Damage = damage;
        Dirty(ent, damageComp);
    }
}

