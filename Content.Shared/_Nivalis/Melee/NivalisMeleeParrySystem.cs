using Content.Shared._Nivalis.Melee.Events;
using Content.Shared.Damage.Systems;
using Content.Shared.Effects;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Popups;
using Content.Shared.Stunnable;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Timing;
using System.Diagnostics.CodeAnalysis;

namespace Content.Shared._Nivalis.Melee;

public abstract partial class SharedNivalisMeleeParrySystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] protected SharedAudioSystem Audio = default!;
    [Dependency] protected SharedHandsSystem Hands = default!;
    [Dependency] protected SharedStunSystem Stun = default!;
    [Dependency] protected SharedPopupSystem Popup = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly SharedColorFlashEffectSystem _color = default!;

    private EntityQuery<NivalisMeleeComponent> _meleeQuery = default!;

    protected IGameTiming Timing => _timing;

    public override void Initialize()
    {
        base.Initialize();

        _meleeQuery = GetEntityQuery<NivalisMeleeComponent>();

        SubscribeAllEvent<NivalisMeleeParryEvent>(OnParryInput);
        SubscribeLocalEvent<NivalisMeleeParryAttemptEvent>(OnParryAttempt);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (!_net.IsServer)
            return;

        var query = EntityQueryEnumerator<NivalisMeleeParryComponent>();
        while (query.MoveNext(out var uid, out var parry))
        {
            if (!parry.Protecting || parry.ParriedThisStance)
                continue;

            if (_timing.CurTime > parry.ParryWindowEnd)
            {
                ApplyFailedParry(uid, parry);
                parry.Protecting = false;
                Dirty(uid, parry);
            }
        }
    }

    public bool IsActivelyParrying(EntityUid uid, NivalisMeleeParryComponent? parry = null)
    {
        if (!TryGetParryComp(uid, out parry))
            return false;

        return parry.Protecting
               && parry.NextParry <= _timing.CurTime
               && _timing.CurTime <= parry.ParryWindowEnd;
    }

    public bool CanParry(EntityUid uid, NivalisMeleeParryComponent? parry = null)
    {
        return TryGetParryComp(uid, out _);
    }

    private bool TryGetParryComp(EntityUid uid, [NotNullWhen(true)] out NivalisMeleeParryComponent? parry)
    {
        if (Hands.TryGetActiveItem(uid, out var held) &&
            _meleeQuery.HasComponent(held) &&
            TryComp<NivalisMeleeParryComponent>(held, out parry))
        {
            return true;
        }

        if (_meleeQuery.HasComponent(uid))
        {
            parry = null;
            return TryComp<NivalisMeleeParryComponent>(uid, out parry);
        }

        parry = null;
        return false;
    }

    private void OnParryAttempt(NivalisMeleeParryAttemptEvent args)
    {
        var victim = args.Victim;

        if (!TryGetParryOwner(victim, out var parryOwner, out var parry))
            return;

        if (!IsActivelyParrying(victim, parry))
            return;

        args.Parried = true;

        parry.ParriedThisStance = true;

        parry.NextParry = TimeSpan.Zero;

        parry.Protecting = false;
        Dirty(parryOwner, parry);

        Stun.TryAddParalyzeDuration(args.Attacker, TimeSpan.FromSeconds(parry.StunDuration), visualized: true);

        if (_net.IsServer)
        {
            Popup.PopupEntity(Loc.GetString("nivalis-parry-success"), victim, victim);
            var filter = Filter.Pvs(victim, entityManager: EntityManager);
            _color.RaiseEffect(new Color(0.35f, 0.55f, 1f), new List<EntityUid> { victim }, filter);

            if (parry.Sound != null)
                Audio.PlayPvs(parry.Sound, victim);
        }
    }

    private void OnParryInput(NivalisMeleeParryEvent msg, EntitySessionEventArgs args)
    {
        if (!msg.Active)
            return;

        var sessionEnt = args.SenderSession.AttachedEntity;
        if (sessionEnt == null)
            return;

        var uid = sessionEnt.Value;

        if (!TryGetParryOwner(uid, out var parryOwner, out var parry))
            return;

        if (parry.NextParry > _timing.CurTime || parry.Protecting)
            return;

        parry.Protecting = true;
        parry.ParriedThisStance = false;
        parry.ParryWindowEnd = _timing.CurTime + TimeSpan.FromSeconds(parry.ParryWindow);
        Dirty(parryOwner, parry);

        if (_net.IsServer)
            Popup.PopupEntity(Loc.GetString("nivalis-parry-ready"), uid, uid);
    }

    private bool TryGetParryOwner(EntityUid uid, out EntityUid owner, [NotNullWhen(true)] out NivalisMeleeParryComponent? parry)
    {
        if (Hands.TryGetActiveItem(uid, out var held) &&
            _meleeQuery.HasComponent(held) &&
            TryComp<NivalisMeleeParryComponent>(held, out parry))
        {
            owner = held.Value;
            return true;
        }

        if (TryComp<NivalisMeleeParryComponent>(uid, out parry))
        {
            owner = uid;
            return true;
        }

        owner = default;
        parry = null;
        return false;
    }

    private void ApplyFailedParry(EntityUid uid, NivalisMeleeParryComponent parry)
    {
        parry.NextParry = _timing.CurTime + TimeSpan.FromSeconds(parry.ParryCooldown);

        Dirty(uid, parry);

        if (!TryGetMeleeWeapon(uid, out var weaponUid, out var weapon))
            return;

        weapon.NextAttack = _timing.CurTime + TimeSpan.FromSeconds(parry.FailedParryPenalty);
        Dirty(weaponUid, weapon);

        if (_net.IsServer)
        {
            var target = GetHolder(uid) ?? uid;
            Popup.PopupEntity(Loc.GetString("nivalis-parry-failed"), target, target);
        }
    }

    private EntityUid? GetHolder(EntityUid uid)
    {
        if (_container.TryGetContainingContainer((uid, null, null), out var container) &&
            Hands.IsHolding(container.Owner, uid))
        {
            return container.Owner;
        }

        if (HasComp<NivalisMeleeParryComponent>(uid))
            return uid;

        return null;
    }

    private bool TryGetMeleeWeapon(EntityUid entity, out EntityUid weaponUid, [NotNullWhen(true)] out NivalisMeleeComponent? melee)
    {
        weaponUid = default;
        melee = null;

        if (Hands.TryGetActiveItem(entity, out var held))
        {
            if (TryComp<NivalisMeleeComponent>(held, out melee))
            {
                weaponUid = held.Value;
                return true;
            }
        }

        if (TryComp<NivalisMeleeComponent>(entity, out melee))
        {
            weaponUid = entity;
            return true;
        }

        return false;
    }

    public bool TryParry(EntityUid victim, EntityUid attacker, EntityUid weapon)
    {
        var ev = new NivalisMeleeParryAttemptEvent(victim, attacker, weapon);
        RaiseLocalEvent(victim, ev);
        return ev.Parried;
    }
}
