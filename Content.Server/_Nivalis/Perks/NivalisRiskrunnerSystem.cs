using Content.Shared._Nivalis.Perks;
using Content.Shared.Hands;
using Content.Shared.Hands.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Interaction.Components;
using Content.Shared.Interaction.Events;
using Content.Shared.Popups;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Server._Nivalis.Perks;

public sealed partial class NivalisRiskrunnerSystem : EntitySystem
{
    public const string RiskrunnerPerk = "Riskrunner";

    [Dependency] private SharedHandsSystem _hands = default!;
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private IPrototypeManager _proto = default!;
    [Dependency] private SharedPopupSystem _popup = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeNetworkEvent<NivalisPerkAbilityPressedMessage>(OnAbilityPressed);

        SubscribeLocalEvent<NivalisRiskrunnerComponent, ComponentShutdown>(OnShutdown);

        SubscribeLocalEvent<NivalisRiskrunnerWeaponComponent, HandDeselectedEvent>(OnWeaponDeselected);
        SubscribeLocalEvent<NivalisRiskrunnerWeaponComponent, GotUnequippedHandEvent>(OnWeaponUnequipped);
        SubscribeLocalEvent<NivalisRiskrunnerWeaponComponent, DroppedEvent>(OnWeaponDropped);
    }

    private bool TryGetRiskrunner(EntityUid uid)
    {
        return TryComp<NivalisPerkComponent>(uid, out var perk) && perk.Perk?.Id == RiskrunnerPerk;
    }

    private void OnAbilityPressed(NivalisPerkAbilityPressedMessage msg, EntitySessionEventArgs args)
    {
        if (args.SenderSession.AttachedEntity is not { } uid)
            return;

        if (!TryGetRiskrunner(uid))
            return;

        var comp = EnsureComp<NivalisRiskrunnerComponent>(uid);
        Dirty(uid, comp);

        if (comp.ActiveWeapon is { } active && !Deleted(active) && !Terminating(active))
        {
            Retract(uid, comp);
            return;
        }

        Summon(uid, comp);
    }

    private void Summon(EntityUid user, NivalisRiskrunnerComponent comp)
    {
        if (!TryComp<NivalisPerkComponent>(user, out var perk) ||
            perk.Perk is not { } perkId ||
            !_proto.TryIndex(perkId, out var proto) ||
            proto.SummonWeapon is not { } weaponProto)
        {
            return;
        }

        if (comp.Charge < comp.SummonThreshold)
        {
            _popup.PopupEntity(Loc.GetString("nivalis-riskrunner-low-charge",
                ("percent", (int)MathF.Round(comp.SummonThreshold))), user, user);
            return;
        }

        if (!TryComp<HandsComponent>(user, out var hands) || hands.ActiveHandId is not { } handId)
            return;

        if (!_hands.HandIsEmpty((user, hands), handId))
        {
            _popup.PopupEntity(Loc.GetString("nivalis-riskrunner-hand-full"), user, user);
            return;
        }

        var weapon = Spawn(weaponProto, Transform(user).Coordinates);
        var weaponComp = EnsureComp<NivalisRiskrunnerWeaponComponent>(weapon);
        weaponComp.Owner = user;

        if (!_hands.TryForcePickup((user, hands), weapon, handId, checkActionBlocker: false))
        {
            QueueDel(weapon);
            return;
        }

        EnsureComp<UnremoveableComponent>(weapon);

        comp.ActiveWeapon = weapon;
        comp.ActiveHand = handId;
        comp.HasDuration = proto.SummonDuration > 0f;
        comp.ExpiresAt = comp.HasDuration
            ? _timing.CurTime + TimeSpan.FromSeconds(proto.SummonDuration)
            : TimeSpan.Zero;
        Dirty(user, comp);
        Dirty(weapon, weaponComp);
    }

    private void Retract(EntityUid user, NivalisRiskrunnerComponent comp)
    {
        var weapon = comp.ActiveWeapon;
        comp.ActiveWeapon = null;
        comp.ActiveHand = null;
        comp.HasDuration = false;
        comp.ExpiresAt = TimeSpan.Zero;
        Dirty(user, comp);

        if (weapon is not { } weaponUid || Deleted(weaponUid) || Terminating(weaponUid))
            return;

        RemComp<UnremoveableComponent>(weaponUid);
        QueueDel(weaponUid);
    }

    private void OnShutdown(Entity<NivalisRiskrunnerComponent> ent, ref ComponentShutdown args)
    {
        if (ent.Comp.ActiveWeapon is not { } weapon || Deleted(weapon) || Terminating(weapon))
            return;

        RemComp<UnremoveableComponent>(weapon);
        QueueDel(weapon);
    }

    private void OnWeaponDeselected(Entity<NivalisRiskrunnerWeaponComponent> ent, ref HandDeselectedEvent args)
    {
        if (TryComp<NivalisRiskrunnerComponent>(args.User, out var comp))
            Retract(args.User, comp);
    }

    private void OnWeaponUnequipped(Entity<NivalisRiskrunnerWeaponComponent> ent, ref GotUnequippedHandEvent args)
    {
        if (TryComp<NivalisRiskrunnerComponent>(args.User, out var comp))
            Retract(args.User, comp);
    }

    private void OnWeaponDropped(Entity<NivalisRiskrunnerWeaponComponent> ent, ref DroppedEvent args)
    {
        if (TryComp<NivalisRiskrunnerComponent>(args.User, out var comp))
            Retract(args.User, comp);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var now = _timing.CurTime;
        var query = EntityQueryEnumerator<NivalisRiskrunnerComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            var weapon = comp.ActiveWeapon;
            var summoned = weapon is { } weaponUid && !Deleted(weaponUid) && !Terminating(weaponUid);

            if (summoned)
            {
                var shotCost = 1f;
                if (TryComp<NivalisRiskrunnerWeaponComponent>(weapon, out var weaponComp) && weaponComp.ShotCost > 0f)
                    shotCost = weaponComp.ShotCost;

                if (comp.Charge < shotCost)
                {
                    comp.Charge = 0f;
                    Retract(uid, comp);
                    continue;
                }

                if (comp.HasDuration && now >= comp.ExpiresAt)
                {
                    Retract(uid, comp);
                    continue;
                }

                continue;
            }

            if (comp.ActiveWeapon != null)
            {
                comp.ActiveWeapon = null;
                comp.ActiveHand = null;
                comp.HasDuration = false;
                comp.ExpiresAt = TimeSpan.Zero;
                Dirty(uid, comp);
            }

            if (comp.RechargeRate > 0f && comp.Charge < 100f)
            {
                comp.Charge = Math.Clamp(comp.Charge + comp.RechargeRate * frameTime, 0f, 100f);
                Dirty(uid, comp);
            }
        }
    }
}
