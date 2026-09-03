using Content.Shared._Nivalis.Perks;
using Content.Shared.Damage;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Systems;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Server._Nivalis.Perks;

public sealed partial class NivalisPerkSystem : SharedNivalisPerkSystem
{
    [Dependency] private DamageableSystem _damageable = default!;
    
    [Dependency] private MobStateSystem _mobState = default!;
    [Dependency] private IGameTiming _timing = default!;

    private readonly Dictionary<EntityUid, TimeSpan> _nextHealthRegen = new();

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<NivalisPerkComponent, DamageModifyEvent>(OnDamageTaken);
        SubscribeLocalEvent<NivalisPerkComponent, ComponentShutdown>(OnShutdown);
        SubscribeNetworkEvent<NivalisPerkAbilityPressedMessage>(OnAbilityPressed);
    }

    private void OnAbilityPressed(NivalisPerkAbilityPressedMessage msg, EntitySessionEventArgs args)
    {
        if (args.SenderSession.AttachedEntity is not { } uid)
            return;

        if (!TryComp<NivalisPerkComponent>(uid, out var perk) || perk.Perk == null)
            return;

        if (_timing.CurTime < perk.NextAbilityUse)
            return;

        perk.NextAbilityUse = _timing.CurTime + TimeSpan.FromSeconds(perk.AbilityCooldown);
        Dirty(uid, perk);

        RaiseLocalEvent(uid, new NivalisPerkUsedEvent(perk.Perk.Value));
    }

    private void OnShutdown(Entity<NivalisPerkComponent> ent, ref ComponentShutdown args)
    {
        _nextHealthRegen.Remove(ent.Owner);
    }

    private void OnDamageTaken(Entity<NivalisPerkComponent> ent, ref DamageModifyEvent args)
    {
        if (ent.Comp.DamageTakenMult != 1f)
            args.Damage = args.Damage * ent.Comp.DamageTakenMult;
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<NivalisPerkComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            if (comp.HealthRegenPerTick <= 0f || comp.HealthRegenInterval <= 0f)
            {
                _nextHealthRegen.Remove(uid);
                continue;
            }

            if (!_nextHealthRegen.TryGetValue(uid, out var next))
            {
                _nextHealthRegen[uid] = _timing.CurTime + TimeSpan.FromSeconds(comp.HealthRegenInterval);
                continue;
            }

            if (_timing.CurTime < next)
                continue;

            if (!_mobState.IsDead(uid) && HasComp<DamageableComponent>(uid))
            {
                _damageable.TryChangeDamage(uid, new DamageSpecifier
                {
                    DamageDict = { ["Brute"] = -comp.HealthRegenPerTick },
                }, ignoreResistances: true);
            }

            _nextHealthRegen[uid] = _timing.CurTime + TimeSpan.FromSeconds(comp.HealthRegenInterval);
        }
    }

    public void SetPerk(Entity<NivalisPerkComponent?> ent, ProtoId<NivalisPerkPrototype>? perk)
    {
        if (!Resolve(ent, ref ent.Comp, false))
        {
            if (perk == null)
                return;
            ent.Comp = AddComp<NivalisPerkComponent>(ent);
        }

        var safe = new Entity<NivalisPerkComponent>(ent, ent.Comp!);
        if (safe.Comp.Perk == perk)
        {
            Recalculate(safe);
            Dirty(safe);
            return;
        }

        safe.Comp.Perk = perk;
        Recalculate(safe);
        Dirty(safe);
    }

    public bool HasPerk(Entity<NivalisPerkComponent?> ent)
    {
        return Resolve(ent, ref ent.Comp, false) && ent.Comp!.Perk != null;
    }

    public void ClearPerk(Entity<NivalisPerkComponent?> ent)
    {
        if (!Resolve(ent, ref ent.Comp, false))
            return;

        var safe = new Entity<NivalisPerkComponent>(ent, ent.Comp!);
        safe.Comp.Perk = null;
        Recalculate(safe);
        Dirty(safe);
    }
}


