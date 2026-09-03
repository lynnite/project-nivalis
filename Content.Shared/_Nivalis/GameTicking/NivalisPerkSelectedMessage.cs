using Content.Shared._Nivalis.Perks;

using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._Nivalis.GameTicking;

[Serializable, NetSerializable]
public sealed class NivalisPerkSelectedMessage : EntityEventArgs
{
    public ProtoId<NivalisPerkPrototype>? Perk;
}
