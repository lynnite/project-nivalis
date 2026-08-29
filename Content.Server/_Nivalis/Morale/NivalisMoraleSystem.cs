using Content.Shared._Nivalis.Melee;
using Content.Shared._Nivalis.Morale;
using Content.Shared._Nivalis.Survivor.Components;
using Content.Shared.Alert;
using Content.Shared.Damage;
using Content.Shared.Damage.Prototypes;
using Content.Shared.Damage.Systems;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Systems;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Server._Nivalis.Morale;

public sealed partial class NivalisMoraleSystem : EntitySystem
{
    public static readonly ProtoId<AlertPrototype> MoraleAlert = "NivalisMorale";
    public static readonly TimeSpan MoraleDuration = TimeSpan.FromMinutes(5);

    private static readonly float PenaltyPerStack = 0.10f;

    private readonly Dictionary<int, DamageModifierSet> _meleeModSetCache = new();
    private readonly Dictionary<int, float> _defenseMultCache = new();

    [Dependency] private AlertsSystem _alerts = default!;
    [Dependency] private MobStateSystem _mobState = default!;
    [Dependency] private IPrototypeManager _proto = default!;
    [Dependency] private IGameTiming _timing = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<NivalisMoraleComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<NivalisMoraleComponent, DamageModifyEvent>(OnDamageTaken);
        SubscribeLocalEvent<MobStateChangedEvent>(OnMobStateChanged);
        SubscribeLocalEvent<NivalisMeleeHitEvent>(OnMeleeHit);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);
        var query = EntityQueryEnumerator<NivalisMoraleComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            if (comp.Morale <= 0)
                continue;

            if (_timing.CurTime < comp.NextResetTime)
                continue;

            ResetMorale((uid, comp));
        }
    }

    private void OnMapInit(Entity<NivalisMoraleComponent> ent, ref MapInitEvent args)
    {
        ent.Comp.Morale = 0;
        ent.Comp.NextResetTime = TimeSpan.Zero;
        _alerts.ShowAlert(ent.Owner, MoraleAlert, severity: (short) ent.Comp.Morale);
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

            AddMorale((uid, comp));
        }
    }

    private void OnMeleeHit(NivalisMeleeHitEvent ev)
    {
        if (!ev.IsHit)
            return;

        if (!TryComp<NivalisMoraleComponent>(ev.User, out var morale))
            return;

        if (morale.Morale <= 0)
            return;

        ev.ModifiersList.Add(GetMeleeModifierSet(morale.Morale));
    }

    private void OnDamageTaken(Entity<NivalisMoraleComponent> ent, ref DamageModifyEvent args)
    {
        if (ent.Comp.Morale <= 0)
            return;

        args.Damage = args.Damage * GetDefenseMultiplier(ent.Comp.Morale);
    }

    private void AddMorale(Entity<NivalisMoraleComponent> ent)
    {
        if (ent.Comp.Morale >= ent.Comp.MaxMorale)
            return;

        var old = ent.Comp.Morale;
        ent.Comp.Morale = Math.Min(ent.Comp.MaxMorale, ent.Comp.Morale + 1);
        ent.Comp.NextResetTime = _timing.CurTime + MoraleDuration;

        ShowAlert(ent);
        Dirty(ent);

        var changeEv = new NivalisMoraleChangedEvent(ent.Owner, old, ent.Comp.Morale);
        RaiseLocalEvent(ref changeEv);
    }

    private void ShowAlert(Entity<NivalisMoraleComponent> ent)
    {
        _alerts.ShowAlert(ent.Owner, MoraleAlert, severity: (short) ent.Comp.Morale);
    }

    private DamageModifierSet GetMeleeModifierSet(int stacks)
    {
        if (!_meleeModSetCache.TryGetValue(stacks, out var set))
        {
            var coefficient = 1f - PenaltyPerStack * stacks;
            var coefficients = new Dictionary<ProtoId<DamageTypePrototype>, float>();

            foreach (var type in _proto.EnumeratePrototypes<DamageTypePrototype>())
                coefficients[new ProtoId<DamageTypePrototype>(type.ID)] = coefficient;

            set = new DamageModifierSet { Coefficients = coefficients };
            _meleeModSetCache[stacks] = set;
        }

        return set;
    }

    private float GetDefenseMultiplier(int stacks)
    {
        if (!_defenseMultCache.TryGetValue(stacks, out var mult))
        {
            mult = 1f + PenaltyPerStack * stacks;
            _defenseMultCache[stacks] = mult;
        }

        return mult;
    }

    public void ModifyMorale(Entity<NivalisMoraleComponent> ent, int delta)
    {
        if (delta == 0)
            return;

        var old = ent.Comp.Morale;
        ent.Comp.Morale = Math.Clamp(ent.Comp.Morale + delta, 0, ent.Comp.MaxMorale);

        if (old == ent.Comp.Morale)
            return;

        if (ent.Comp.Morale > 0)
            ent.Comp.NextResetTime = _timing.CurTime + MoraleDuration;

        ShowAlert(ent);
        Dirty(ent);

        var changeEv = new NivalisMoraleChangedEvent(ent.Owner, old, ent.Comp.Morale);
        RaiseLocalEvent(ref changeEv);
    }

    private void ResetMorale(Entity<NivalisMoraleComponent> ent)
    {
        if (ent.Comp.Morale <= 0)
            return;

        var old = ent.Comp.Morale;
        ent.Comp.Morale = 0;
        ent.Comp.NextResetTime = TimeSpan.Zero;

        ShowAlert(ent);
        Dirty(ent);

        var changeEv = new NivalisMoraleChangedEvent(ent.Owner, old, ent.Comp.Morale);
        RaiseLocalEvent(ref changeEv);
    }
}
