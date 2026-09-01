using System.Linq;
using Content.Shared._Nivalis.Traits;
using Content.Shared.Damage;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Prototypes;
using Content.Shared.Damage.Systems;
using Content.Shared.Mobs.Systems;
using Content.Shared.Movement.Systems;
using Content.Shared.StatusEffectNew;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server._Nivalis.Traits;

public sealed partial class NivalisTraitSystem : SharedNivalisTraitSystem
{
    public static readonly EntProtoId SpeedEffect = "StatusEffectNivalisFrenzied";

    private static readonly ProtoId<DamageTypePrototype>[] FirearmDamageTypes =
    [
        new("Bullet"),
        new("Piercing"),
    ];

    private static readonly ProtoId<DamageTypePrototype>[] ExplosiveDamageTypes =
    [
        new("Heat"),
        new("Shock"),
        new("Radiation"),
        new("Caustic"),
        new("Structural"),
    ];

    [Dependency] private MovementModStatusSystem _movementMod = default!;
    [Dependency] private StatusEffectsSystem _status = default!;
    [Dependency] private IPrototypeManager _traitProto = default!;
    [Dependency] private DamageableSystem _damageable = default!;
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private MobStateSystem _mobState = default!;

    private readonly Dictionary<EntityUid, TimeSpan> _nextHealthRegen = new();

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<NivalisTraitComponent, BeforeDamageChangedEvent>(OnBeforeDamage);
        SubscribeLocalEvent<NivalisTraitComponent, DamageModifyEvent>(OnDamageTaken);
        SubscribeLocalEvent<NivalisTraitComponent, ComponentShutdown>(OnShutdown);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<NivalisTraitComponent>();
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

            TryApplyHealthRegen(uid);
            _nextHealthRegen[uid] = _timing.CurTime + TimeSpan.FromSeconds(comp.HealthRegenInterval);
        }
    }

    private void TryApplyHealthRegen(EntityUid uid)
    {
        if (!TryComp<NivalisTraitComponent>(uid, out var comp))
            return;

        if (comp.HealthRegenPerTick <= 0f)
            return;

        if (!HasComp<DamageableComponent>(uid) || _mobState.IsDead(uid))
            return;

        _damageable.TryChangeDamage(uid, new DamageSpecifier
        {
            DamageDict = { ["Brute"] = -comp.HealthRegenPerTick },
        }, ignoreResistances: true);
    }

    private void OnShutdown(Entity<NivalisTraitComponent> ent, ref ComponentShutdown args)
    {
        _status.TryRemoveStatusEffect(ent.Owner, SpeedEffect);
        _nextHealthRegen.Remove(ent.Owner);
    }

    private void OnBeforeDamage(Entity<NivalisTraitComponent> ent, ref BeforeDamageChangedEvent args)
    {
        if (args.Origin != ent.Owner || !args.Damage.AnyPositive())
            return;

        if (ent.Comp.DamageDealtMult != 1f)
            args.Damage = args.Damage * ent.Comp.DamageDealtMult;
    }

    private void OnDamageTaken(Entity<NivalisTraitComponent> ent, ref DamageModifyEvent args)
    {
        if (ent.Comp.DamageTakenMult != 1f)
            args.Damage = args.Damage * ent.Comp.DamageTakenMult;

        if (ent.Comp.FirearmExplosiveDamageTakenMult != 1f && args.Damage.AnyPositive())
        {
            var dict = args.Damage.DamageDict;
            foreach (var type in FirearmDamageTypes)
            {
                if (dict.TryGetValue(type, out var value) && value > 0)
                    dict[type] *= ent.Comp.FirearmExplosiveDamageTakenMult;
            }
            foreach (var type in ExplosiveDamageTypes)
            {
                if (dict.TryGetValue(type, out var value) && value > 0)
                    dict[type] *= ent.Comp.FirearmExplosiveDamageTakenMult;
            }
        }
    }

    public bool AddTrait(Entity<NivalisTraitComponent?> ent, ProtoId<NivalisTraitPrototype> trait)
    {
        if (!Resolve(ent, ref ent.Comp, false))
            return false;

        if (ent.Comp.Traits.Contains(trait))
            return true;

        if (ent.Comp.Traits.Count >= ent.Comp.MaxTraits)
            return false;

        ent.Comp.Traits.Add(trait);
        Dirty(ent);
        ApplyTraitModifiers((ent.Owner, ent.Comp));
        return true;
    }

    public bool RemoveTrait(Entity<NivalisTraitComponent?> ent, ProtoId<NivalisTraitPrototype> trait)
    {
        if (!Resolve(ent, ref ent.Comp, false))
            return false;

        if (!ent.Comp.Traits.Remove(trait))
            return false;

        Dirty(ent);
        ApplyTraitModifiers((ent.Owner, ent.Comp));
        return true;
    }

    public ProtoId<NivalisTraitPrototype>? GrantRandomTrait(Entity<NivalisTraitComponent?> ent, IRobustRandom random)
    {
        if (!Resolve(ent, ref ent.Comp, false))
            return null;

        if (ent.Comp.Traits.Count >= ent.Comp.MaxTraits)
            return null;

        var candidates = _traitProto.EnumeratePrototypes<NivalisTraitPrototype>()
            .Select(p => new ProtoId<NivalisTraitPrototype>(p.ID))
            .Where(id => !ent.Comp.Traits.Contains(id))
            .ToList();

        if (candidates.Count == 0)
            return null;

        var granted = candidates[random.Next(0, candidates.Count)];
        ent.Comp.Traits.Add(granted);
        Dirty(ent);
        ApplyTraitModifiers((ent.Owner, ent.Comp));
        return granted;
    }

    public List<ProtoId<NivalisTraitPrototype>> GetDraftChoices(Entity<NivalisTraitComponent?> ent, int count, IRobustRandom random)
    {
        if (!Resolve(ent, ref ent.Comp, false))
            return new();

        if (ent.Comp.Traits.Count >= ent.Comp.MaxTraits)
            return new();

        return _traitProto.EnumeratePrototypes<NivalisTraitPrototype>()
            .Select(p => new ProtoId<NivalisTraitPrototype>(p.ID))
            .Where(id => !ent.Comp.Traits.Contains(id))
            .OrderBy(_ => random.Next())
            .Take(count)
            .ToList();
    }

    public void ApplyTraitModifiers(Entity<NivalisTraitComponent> ent)
    {
        if (Recalculate(ent))
            Dirty(ent);

        if (ent.Comp.SpeedMult != 1f)
        {
            _movementMod.TryAddMovementSpeedModDuration(ent.Owner, SpeedEffect, TimeSpan.FromSeconds(60), ent.Comp.SpeedMult, ent.Comp.SpeedMult);
        }
        else
        {
            _status.TryRemoveStatusEffect(ent.Owner, SpeedEffect);
        }

        if (ent.Comp.MaxHealthBonus > 0f)
        {
            _damageable.TryChangeDamage(ent.Owner, new DamageSpecifier
            {
                DamageDict = { ["Brute"] = -ent.Comp.MaxHealthBonus },
            }, ignoreResistances: true);
        }
    }
}

