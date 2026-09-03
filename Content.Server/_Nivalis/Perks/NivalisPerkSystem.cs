using Content.Server._Nivalis.Perks;
using Content.Shared._Nivalis.Perks;
using Robust.Shared.Prototypes;

namespace Content.Server._Nivalis.Perks;

public sealed partial class NivalisPerkSystem : SharedNivalisPerkSystem
{
    [Dependency] private IPrototypeManager _proto = default!;

    public override void Initialize()
    {
        base.Initialize();
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

