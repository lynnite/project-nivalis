using Content.Shared._Nivalis.Melee;
using Content.Shared.Movement.Systems;
using Content.Shared.StatusEffectNew;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Shared._Nivalis.Status;

public sealed partial class NivalisFractureSystem : EntitySystem
{
    public const string ArmEffectId = "StatusEffectNivalisFractureArm";
    public const string LegEffectId = "StatusEffectNivalisFractureLeg";

    private static readonly EntProtoId ArmEffect = ArmEffectId;
    private static readonly EntProtoId LegEffect = LegEffectId;
    private static readonly TimeSpan FractureDuration = TimeSpan.FromMinutes(10);

    [Dependency] private StatusEffectsSystem _status = default!;
    [Dependency] private MovementSpeedModifierSystem _movementSpeed = default!;
    [Dependency] private IRobustRandom _random = default!;

    private EntityQuery<NivalisFractureComponent> _fractureQuery = default!;

    public override void Initialize()
    {
        base.Initialize();

        _fractureQuery = GetEntityQuery<NivalisFractureComponent>();

        SubscribeLocalEvent<NivalisMeleeHitEvent>(OnMeleeHit);
        SubscribeLocalEvent<NivalisFractureComponent, RefreshMovementSpeedModifiersEvent>(OnRefreshSpeed);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        List<EntityUid>? toRemove = null;
        var query = EntityQueryEnumerator<NivalisFractureComponent>();
        while (query.MoveNext(out var uid, out _))
        {
            if (HasArmFracture(uid) || HasLegFracture(uid))
                continue;

            (toRemove ??= new()).Add(uid);
        }

        if (toRemove != null)
        {
            foreach (var uid in toRemove)
            {
                RemComp<NivalisFractureComponent>(uid);
                _movementSpeed.RefreshMovementSpeedModifiers(uid);
            }
        }
    }

    private void OnMeleeHit(NivalisMeleeHitEvent args)
    {
        if (!TryComp<NivalisFracturerComponent>(args.Weapon, out var fracturer))
            return;

        foreach (var target in args.HitEntities)
        {
            if (!_random.Prob(fracturer.FractureChance))
                continue;

            if (_random.Prob(0.5f))
                ApplyFracture(target, ArmEffect);
            else
                ApplyFracture(target, LegEffect);
        }
    }

    private void ApplyFracture(EntityUid target, EntProtoId effect)
    {
        if (HasFracture(target, effect))
            return;

        if (_status.TryAddStatusEffectDuration(target, effect, FractureDuration))
            RefreshFractureState(target);
    }

    public bool HasArmFracture(EntityUid uid)
    {
        return HasFracture(uid, ArmEffect);
    }

    public bool HasLegFracture(EntityUid uid)
    {
        return HasFracture(uid, LegEffect);
    }

    private bool HasFracture(EntityUid uid, EntProtoId effect)
    {
        return _status.HasStatusEffect(uid, effect);
    }

    private void OnRefreshSpeed(EntityUid uid, NivalisFractureComponent comp, ref RefreshMovementSpeedModifiersEvent args)
    {
        if (comp.LegFractured)
        {
            const float slow = 0.55f;
            args.ModifySpeed(slow, slow);
        }
    }

    public void RefreshFractureState(EntityUid uid)
    {
        var hadComponent = _fractureQuery.HasComp(uid);
        var hasArm = HasArmFracture(uid);
        var hasLeg = HasLegFracture(uid);

        if (!hasArm && !hasLeg)
        {
            if (hadComponent)
            {
                RemComp<NivalisFractureComponent>(uid);
                _movementSpeed.RefreshMovementSpeedModifiers(uid);
            }
            return;
        }

        var comp = EnsureComp<NivalisFractureComponent>(uid);
        var dirty = false;

        if (comp.ArmFractured != hasArm)
        {
            comp.ArmFractured = hasArm;
            dirty = true;
        }

        if (comp.LegFractured != hasLeg)
        {
            comp.LegFractured = hasLeg;
            dirty = true;
        }

        if (dirty)
            Dirty(uid, comp);

            _movementSpeed.RefreshMovementSpeedModifiers(uid);
    }
}


