using Robust.Shared.Prototypes;

namespace Content.Shared._Nivalis.Perks;


public sealed class NivalisPerkUsedEvent : EntityEventArgs
{
    public NivalisPerkUsedEvent(EntityUid user, ProtoId<NivalisPerkPrototype> perk)
    {
        User = user;
        Perk = perk;
    }

    public EntityUid User { get; }
    public ProtoId<NivalisPerkPrototype> Perk { get; }
}
