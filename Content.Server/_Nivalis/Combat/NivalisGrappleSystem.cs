using Content.Shared._Nivalis.Combat;
using Content.Shared.DoAfter;
using Content.Shared.Effects;
using Content.Shared.Popups;
using Content.Shared.Stunnable;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Map;
using Robust.Shared.Player;
using Robust.Shared.Timing;

namespace Content.Server._Nivalis.Combat;

/// <summary>
///     Server-side grappling for NPCs. The NPC initiates a short do-after (during which the
///     victim sees a ramping red warning hue) and, if it completes, stuns the victim.
/// </summary>
public sealed partial class NivalisGrappleSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly SharedStunSystem _stun = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedColorFlashEffectSystem _color = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;

    private static readonly Color GrappleColor = new(1f, 0.1f, 0.1f);

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<NivalisGrappleComponent, NivalisGrappleDoAfterEvent>(OnGrappleComplete);
    }

    /// <summary>
    ///     Attempts to have <paramref name="user"/> begin grappling <paramref name="target"/>.
    /// </summary>
    public bool TryStartGrapple(EntityUid user, EntityUid target)
    {
        if (!TryComp<NivalisGrappleComponent>(user, out var grapple) ||
            grapple.NextAttempt > _timing.CurTime)
        {
            return false;
        }

        if (!TryComp<TransformComponent>(user, out var userXform) ||
            !TryComp<TransformComponent>(target, out var targetXform))
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
            // The grapple "locks on": walking away (or the NPC moving in) does not cancel it,
            // and neither does taking damage once started.
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

        // Warning feedback for the victim.
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

        // Stun the victim.
        _stun.TryAddParalyzeDuration(target, TimeSpan.FromSeconds(comp.StunTime), visualized: true);

        // Red flash to accompany the catch.
        if (TryComp<ActorComponent>(target, out _))
        {
            var filter = Filter.Pvs(target, entityManager: EntityManager);
            _color.RaiseEffect(GrappleColor, new List<EntityUid> { target }, filter);
            _popup.PopupEntity(Loc.GetString("nivalis-grapple-caught"), target, target, PopupType.LargeCaution);
        }
    }
}
