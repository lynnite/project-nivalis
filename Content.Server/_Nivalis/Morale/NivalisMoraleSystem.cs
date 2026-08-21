using Content.Shared._Nivalis.Morale;
using Content.Shared._Nivalis.Perks;
using Content.Shared._Nivalis.Survivor.Components;
using Content.Shared.Alert;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Systems;
using Content.Shared.Movement.Systems;
using Content.Shared.StatusEffectNew;
using Robust.Shared.Prototypes;

namespace Content.Server._Nivalis.Morale;

/// <summary>
///     Manages <see cref="NivalisMoraleComponent"/> for survs.
///     Morale only drops when a teammate (an entity with <see cref="NivalisSurvivorComponent"/>)
///     dies. Derived morale levels drive movement-modifier effects, broadcast a
///     <see cref="NivalisMoraleChangedEvent"/> for other systems to react to
/// </summary>
public sealed partial class NivalisMoraleSystem : EntitySystem
{
    public static readonly EntProtoId LowMoraleEffect = "StatusEffectNivalisLowMorale";
    public static readonly ProtoId<AlertPrototype> MoraleAlert = "NivalisMorale";

    private static (NivalisMoraleLevel Level, float Walk, float Sprint)[] _levelTable =
    {
        (NivalisMoraleLevel.High,    1.00f, 1.00f),
        (NivalisMoraleLevel.Normal,  0.92f, 0.92f),
        (NivalisMoraleLevel.Low,     0.82f, 0.82f),
        (NivalisMoraleLevel.Critical,0.70f, 0.70f),
    };

    [Dependency] private AlertsSystem _alerts = default!;
    [Dependency] private MobStateSystem _mobState = default!;
    [Dependency] private MovementModStatusSystem _movementMod = default!;
    [Dependency] private StatusEffectsSystem _status = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<NivalisMoraleComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<MobStateChangedEvent>(OnMobStateChanged);
    }

    private void OnMapInit(Entity<NivalisMoraleComponent> ent, ref MapInitEvent args)
    {
        ent.Comp.Morale = ent.Comp.MaxMorale;
        ent.Comp.Level = NivalisMoraleLevel.High;

        _alerts.ShowAlert(ent.Owner, MoraleAlert, severity: (short) ent.Comp.Level);

        RefreshMoraleModifier(ent);
        Dirty(ent);
    }

    private void OnMobStateChanged(MobStateChangedEvent ev)
    {
        if (ev.NewMobState != MobState.Dead)
            return;

        if (!HasComp<NivalisSurvivorComponent>(ev.Target))
            return;

        var query = EntityQueryEnumerator<NivalisMoraleComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            if (uid == ev.Target)
                continue;

            if (!_mobState.IsAlive(uid))
                continue;

            var penalty = comp.DeathPenalty;
            if (TryComp<NivalisPerkComponent>(uid, out var perks) && perks.MoralePenaltyReduction > 0f)
                penalty *= (1f - perks.MoralePenaltyReduction);

            comp.Morale = MathF.Max(0f, comp.Morale - penalty);
            RefreshMoraleLevel((uid, comp));
            Dirty(uid, comp);
        }
    }

    private void RefreshMoraleLevel(Entity<NivalisMoraleComponent> ent)
    {
        var oldLevel = ent.Comp.Level;
        var newLevel = ComputeLevel(ent.Comp.Morale, ent.Comp.MaxMorale);

        if (oldLevel == newLevel)
            return;

        ent.Comp.Level = newLevel;
        RefreshMoraleModifier(ent);
        _alerts.ShowAlert(ent.Owner, MoraleAlert, severity: (short) newLevel);

        var changeEv = new NivalisMoraleChangedEvent(ent.Owner, oldLevel, newLevel);
        RaiseLocalEvent(ref changeEv);
    }

    private static NivalisMoraleLevel ComputeLevel(float morale, float max)
    {
        var fraction = morale / MathF.Max(1f, max);
        if (fraction >= 0.75f)
            return NivalisMoraleLevel.High;
        if (fraction >= 0.5f)
            return NivalisMoraleLevel.Normal;
        if (fraction >= 0.25f)
            return NivalisMoraleLevel.Low;
        return NivalisMoraleLevel.Critical;
    }

    private void RefreshMoraleModifier(Entity<NivalisMoraleComponent> ent)
    {
        foreach (var (level, walk, sprint) in _levelTable)
        {
            if (level == ent.Comp.Level)
            {
                if (MathHelper.CloseToPercent(walk, 1f))
                {
                    _status.TryRemoveStatusEffect(ent.Owner, LowMoraleEffect);
                }
                else
                {
                    _movementMod.TryAddMovementSpeedModDuration(ent.Owner, LowMoraleEffect, TimeSpan.FromSeconds(60), walk, sprint);
                }
                return;
            }
        }
    }

    public void ModifyMorale(Entity<NivalisMoraleComponent> ent, float delta)
    {
        ent.Comp.Morale = Math.Clamp(ent.Comp.Morale + delta, 0f, ent.Comp.MaxMorale);
        RefreshMoraleLevel(ent);
        Dirty(ent);
    }
}

