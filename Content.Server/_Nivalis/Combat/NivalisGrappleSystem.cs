using Content.Shared._Nivalis.Combat;
using Content.Shared.DoAfter;
using Content.Shared.Effects;
using Content.Shared.IdentityManagement;
using Content.Shared.Popups;
using Content.Shared.StatusEffectNew;
using Content.Shared.Stunnable;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Map;
using Robust.Shared.Player;
using Robust.Shared.Timing;

namespace Content.Server._Nivalis.Combat;

public sealed partial class NivalisGrappleSystem : EntitySystem
{
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private SharedDoAfterSystem _doAfter = default!;
    [Dependency] private StatusEffectsSystem _status = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private SharedColorFlashEffectSystem _color = default!;
    [Dependency] private SharedAudioSystem _audio = default!;

    private static readonly Color GrappleColor = new(1f, 0.1f, 0.1f);

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<NivalisGrappleComponent, NivalisGrappleDoAfterEvent>(OnGrappleComplete);
    }

    public bool TryStartGrapple(EntityUid user, EntityUid target)
    {
        if (!TryComp<NivalisGrappleComponent>(user, out var grapple) ||
            grapple.NextAttempt > _timing.CurTime)
        {
            return false;
        }

        if (!TryComp(user, out TransformComponent? userXform) ||
            !TryComp(target, out TransformComponent? targetXform))
        {
            return false;
        }

        if (!userXform.Coordinates.TryDistance(EntityManager, targetXform.Coordinates, out var dist) ||
            dist > grapple.Range)
        {
            return false;
        }

        grapple.NextAttempt = _timing.CurTime + TimeSpan.FromSeconds(grapple.Cooldown);
        Dirty(user, grapple);

        var args = new DoAfterArgs(EntityManager, user, TimeSpan.FromSeconds(grapple.GrappleTime),
            new NivalisGrappleDoAfterEvent(), target: target, used: user, eventTarget: user)
        {
            BreakOnMove = false,
            BreakOnDamage = false,
            RequireCanInteract = false,
            DistanceThreshold = null,
        };

        if (!_doAfter.TryStartDoAfter(args))
        {
            grapple.NextAttempt = TimeSpan.Zero;
            return false;
        }

        if (TryComp<ActorComponent>(target, out _))
        {
            _popup.PopupEntity(Loc.GetString("nivalis-grapple-start"), target, target, PopupType.LargeCaution);
            _audio.PlayPvs(new SoundPathSpecifier("/Audio/Effects/alert.ogg"), user, AudioParams.Default.WithVolume(3f));
        }

        return true;
    }

    private void OnGrappleComplete(EntityUid uid, NivalisGrappleComponent comp, NivalisGrappleDoAfterEvent ev)
    {
        if (ev.Cancelled || ev.Handled || ev.Target is not { } target)
            return;

        ev.Handled = true;

        _status.TryAddStatusEffectDuration(target, SharedStunSystem.StunId, TimeSpan.FromSeconds(comp.StunTime));

        if (TryComp<ActorComponent>(target, out _))
        {
            var filter = Filter.Pvs(target, entityManager: EntityManager);
            _color.RaiseEffect(GrappleColor, new List<EntityUid> { target }, filter);
            _popup.PopupEntity(Loc.GetString("nivalis-grapple-caught", ("target", Identity.Entity(target, EntityManager))), target, target, PopupType.LargeCaution);
        }
    }
}
