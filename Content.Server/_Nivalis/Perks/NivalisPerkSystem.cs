using System.Linq;
using Content.Shared._Nivalis.Perks;
using Content.Shared.Damage.Systems;
using Content.Shared.Movement.Systems;
using Content.Shared.StatusEffectNew;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server._Nivalis.Perks;

public sealed partial class NivalisPerkSystem : SharedNivalisPerkSystem
{
    public static readonly EntProtoId SpeedEffect = "StatusEffectNivalisFrenzied";

    [Dependency] private MovementModStatusSystem _movementMod = default!;
    [Dependency] private StatusEffectsSystem _status = default!;
    [Dependency] private IPrototypeManager _perkProto = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<NivalisPerkComponent, BeforeDamageChangedEvent>(OnBeforeDamage);
        SubscribeLocalEvent<NivalisPerkComponent, DamageModifyEvent>(OnDamageTaken);
        SubscribeLocalEvent<NivalisPerkComponent, ComponentShutdown>(OnShutdown);
    }

    private void OnShutdown(Entity<NivalisPerkComponent> ent, ref ComponentShutdown args)
    {
        _status.TryRemoveStatusEffect(ent.Owner, SpeedEffect);
    }

    private void OnBeforeDamage(Entity<NivalisPerkComponent> ent, ref BeforeDamageChangedEvent args)
    {
        if (args.Origin != ent.Owner || !args.Damage.AnyPositive())
            return;

        if (ent.Comp.DamageDealtMult != 1f)
            args.Damage = args.Damage * ent.Comp.DamageDealtMult;
    }

    private void OnDamageTaken(Entity<NivalisPerkComponent> ent, ref DamageModifyEvent args)
    {
        if (ent.Comp.DamageTakenMult != 1f)
            args.Damage = args.Damage * ent.Comp.DamageTakenMult;
    }

    public bool AddPerk(Entity<NivalisPerkComponent?> ent, ProtoId<NivalisPerkPrototype> perk)
    {
        if (!Resolve(ent, ref ent.Comp, false))
            return false;

        if (ent.Comp.Perks.Contains(perk))
            return true;

        if (ent.Comp.Perks.Count >= ent.Comp.MaxPerks)
            return false;

        ent.Comp.Perks.Add(perk);
        Dirty(ent);
        ApplyPerkModifiers((ent.Owner, ent.Comp));
        return true;
    }

    public bool RemovePerk(Entity<NivalisPerkComponent?> ent, ProtoId<NivalisPerkPrototype> perk)
    {
        if (!Resolve(ent, ref ent.Comp, false))
            return false;

        if (!ent.Comp.Perks.Remove(perk))
            return false;

        Dirty(ent);
        ApplyPerkModifiers((ent.Owner, ent.Comp));
        return true;
    }

    public ProtoId<NivalisPerkPrototype>? GrantRandomPerk(Entity<NivalisPerkComponent?> ent, IRobustRandom random)
    {
        if (!Resolve(ent, ref ent.Comp, false))
            return null;

        if (ent.Comp.Perks.Count >= ent.Comp.MaxPerks)
            return null;

        var candidates = _perkProto.EnumeratePrototypes<NivalisPerkPrototype>()
            .Select(p => new ProtoId<NivalisPerkPrototype>(p.ID))
            .Where(id => !ent.Comp.Perks.Contains(id))
            .ToList();

        if (candidates.Count == 0)
            return null;

        var granted = candidates[random.Next(0, candidates.Count)];
        ent.Comp.Perks.Add(granted);
        Dirty(ent);
        ApplyPerkModifiers((ent.Owner, ent.Comp));
        return granted;
    }

    public List<ProtoId<NivalisPerkPrototype>> GetDraftChoices(Entity<NivalisPerkComponent?> ent, int count, IRobustRandom random)
    {
        if (!Resolve(ent, ref ent.Comp, false))
            return new();

        if (ent.Comp.Perks.Count >= ent.Comp.MaxPerks)
            return new();

        return _perkProto.EnumeratePrototypes<NivalisPerkPrototype>()
            .Select(p => new ProtoId<NivalisPerkPrototype>(p.ID))
            .Where(id => !ent.Comp.Perks.Contains(id))
            .OrderBy(_ => random.Next())
            .Take(count)
            .ToList();
    }

    public void ApplyPerkModifiers(Entity<NivalisPerkComponent> ent)
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
    }
}

