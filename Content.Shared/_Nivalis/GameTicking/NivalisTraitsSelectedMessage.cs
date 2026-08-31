using Content.Shared._Nivalis.Traits;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._Nivalis.GameTicking;

/// <summary>
///     Sent from the lobby when a player changes their selected survivor traits.
/// </summary>
[Serializable, NetSerializable]
public sealed class NivalisTraitsSelectedMessage : EntityEventArgs
{
    public List<ProtoId<NivalisTraitPrototype>> Traits = new();
}
