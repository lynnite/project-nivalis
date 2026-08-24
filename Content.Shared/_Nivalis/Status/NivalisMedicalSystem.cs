using Content.Shared.DoAfter;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Events;
using Content.Shared.Popups;
using Content.Shared.Stacks;
using Content.Shared.StatusEffectNew;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Shared._Nivalis.Status;

public abstract partial class SharedNivalisMedicalSystem : EntitySystem
{
    private static readonly EntProtoId BleedEffect = "StatusEffectNivalisBleed";
    private static readonly EntProtoId BleedImmunityEffect = "StatusEffectNivalisBleedImmunity";
    private static readonly EntProtoId ArteryEffect = "StatusEffectNivalisArtery";
    private static readonly TimeSpan ImmunityDuration = TimeSpan.FromMinutes(2);

    [Dependency] private SharedStackSystem _stack = default!;
    [Dependency] private SharedDoAfterSystem _doAfter = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private StatusEffectsSystem _status = default!;
    [Dependency] private NivalisFractureSystem _fracture = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<NivalisBandageComponent, AfterInteractEvent>(OnBandageInteract);
        SubscribeLocalEvent<NivalisBandageComponent, NivalisBandageDoAfterEvent>(OnBandageDoAfter);
        SubscribeLocalEvent<NivalisSplintComponent, AfterInteractEvent>(OnSplintInteract);
        SubscribeLocalEvent<NivalisSplintComponent, UseInHandEvent>(OnSplintUseInHand);
        SubscribeLocalEvent<NivalisSplintComponent, NivalisSplintDoAfterEvent>(OnSplintDoAfter);
        SubscribeLocalEvent<NivalisTourniquetComponent, AfterInteractEvent>(OnTourniquetInteract);
        SubscribeLocalEvent<NivalisTourniquetComponent, UseInHandEvent>(OnTourniquetUseInHand);
        SubscribeLocalEvent<NivalisTourniquetComponent, NivalisTourniquetDoAfterEvent>(OnTourniquetDoAfter);
    }

    private void OnBandageInteract(Entity<NivalisBandageComponent> bandage, ref AfterInteractEvent args)
    {
        if (args.Handled || args.Target is not { } target || !args.CanReach)
            return;

        var user = args.User;
        var isBleeding = _status.HasStatusEffect(target, BleedEffect);

        if (!isBleeding && !bandage.Comp.Aseptic)
            return;

        args.Handled = true;

        if (!TryStartMedicalDoAfter(user, bandage, target, new NivalisBandageDoAfterEvent()))
            return;

        _popup.PopupEntity(Loc.GetString("nivalis-bandage-start"), user, user);
    }

    private void OnBandageDoAfter(Entity<NivalisBandageComponent> bandage, ref NivalisBandageDoAfterEvent args)
    {
        if (args.Cancelled || args.Handled || args.Target is not { } target)
            return;

        args.Handled = true;

        if (_status.HasStatusEffect(target, BleedEffect))
        {
            _status.TryRemoveStatusEffect(target, BleedEffect);
            _popup.PopupEntity(Loc.GetString("nivalis-bandage-cured"), args.User, args.User);
        }

        if (bandage.Comp.Aseptic)
        {
            if (_status.TryAddStatusEffectDuration(target, BleedImmunityEffect, ImmunityDuration))
                _popup.PopupEntity(Loc.GetString("nivalis-bandage-protected"), args.User, args.User);
        }

        ConsumeStack(bandage);
    }

    private void OnSplintInteract(Entity<NivalisSplintComponent> splint, ref AfterInteractEvent args)
    {
        if (args.Handled || args.Target is not { } target || !args.CanReach)
            return;

        args.Handled = true;
        StartSplint(args.User, splint, target);
    }

    private void OnSplintUseInHand(Entity<NivalisSplintComponent> splint, ref UseInHandEvent args)
    {
        if (args.Handled)
            return;

        args.Handled = true;
        StartSplint(args.User, splint, args.User);
    }

    private void StartSplint(EntityUid user, Entity<NivalisSplintComponent> splint, EntityUid target)
    {
        if (!HasFracture(target))
        {
            _popup.PopupEntity(Loc.GetString("nivalis-splint-none"), user, user);
            return;
        }

        if (!TryStartMedicalDoAfter(user, splint, target, new NivalisSplintDoAfterEvent()))
            return;

        _popup.PopupEntity(Loc.GetString("nivalis-splint-start"), user, user);
    }

    private void OnSplintDoAfter(Entity<NivalisSplintComponent> splint, ref NivalisSplintDoAfterEvent args)
    {
        if (args.Cancelled || args.Handled || args.Target is not { } target)
            return;

        if (!HasFracture(target))
            return;

        args.Handled = true;

        if (_fracture.HasArmFracture(target))
            _status.TryRemoveStatusEffect(target, NivalisFractureSystem.ArmEffectId);

        if (_fracture.HasLegFracture(target))
            _status.TryRemoveStatusEffect(target, NivalisFractureSystem.LegEffectId);

        _fracture.RefreshFractureState(target);

        _popup.PopupEntity(Loc.GetString("nivalis-splint-applied"), args.User, args.User);
        ConsumeStack(splint);
    }

    private bool HasFracture(EntityUid uid)
    {
        return _fracture.HasArmFracture(uid) || _fracture.HasLegFracture(uid);
    }

    private void OnTourniquetInteract(Entity<NivalisTourniquetComponent> tourniquet, ref AfterInteractEvent args)
    {
        if (args.Handled || args.Target is not { } target || !args.CanReach)
            return;

        args.Handled = true;
        StartTourniquet(args.User, tourniquet, target);
    }

    private void OnTourniquetUseInHand(Entity<NivalisTourniquetComponent> tourniquet, ref UseInHandEvent args)
    {
        if (args.Handled)
            return;

        args.Handled = true;
        StartTourniquet(args.User, tourniquet, args.User);
    }

    private void StartTourniquet(EntityUid user, Entity<NivalisTourniquetComponent> tourniquet, EntityUid target)
    {
        if (!_status.HasStatusEffect(target, ArteryEffect) && !_status.HasStatusEffect(target, BleedEffect))
        {
            _popup.PopupEntity(Loc.GetString("nivalis-tourniquet-none"), user, user);
            return;
        }

        if (!TryStartMedicalDoAfter(user, tourniquet, target, new NivalisTourniquetDoAfterEvent()))
            return;

        _popup.PopupEntity(Loc.GetString("nivalis-tourniquet-start"), user, user);
    }

    private void OnTourniquetDoAfter(Entity<NivalisTourniquetComponent> tourniquet, ref NivalisTourniquetDoAfterEvent args)
    {
        if (args.Cancelled || args.Handled || args.Target is not { } target)
            return;

        args.Handled = true;

        var treated = false;

        if (_status.HasStatusEffect(target, ArteryEffect))
        {
            _status.TryRemoveStatusEffect(target, ArteryEffect);
            treated = true;
        }

        if (_status.HasStatusEffect(target, BleedEffect))
        {
            _status.TryRemoveStatusEffect(target, BleedEffect);
            treated = true;
        }

        if (!treated)
        {
            _popup.PopupEntity(Loc.GetString("nivalis-tourniquet-none"), args.User, args.User);
            return;
        }

        _popup.PopupEntity(Loc.GetString("nivalis-tourniquet-applied"), args.User, args.User);
        ConsumeStack(tourniquet);
    }

    private bool TryStartMedicalDoAfter(EntityUid user, EntityUid item, EntityUid target, DoAfterEvent ev)
    {
        var doAfter = new DoAfterArgs(
            EntityManager,
            user,
            1.0f,
            ev,
            eventTarget: item,
            used: item,
            target: target)
        {
            NeedHand = true,
            BreakOnMove = true,
            BreakOnDropItem = true,
            BreakOnHandChange = true,
        };

        return _doAfter.TryStartDoAfter(doAfter);
    }

    private void ConsumeStack(EntityUid item)
    {
        if (!TryComp<StackComponent>(item, out var stack))
        {
            QueueDel(item);
            return;
        }

        _stack.SetCount((item, stack), _stack.GetCount(item) - 1);
    }
}
