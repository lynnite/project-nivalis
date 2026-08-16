using Content.Shared._Nivalis.Perks;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._Nivalis.Traits;

[Serializable, NetSerializable]
public enum NivalisTraitDraftUiKey : byte
{
    Key,
}

[Serializable, NetSerializable]
public sealed class NivalisTraitDraftChoice
{
    public ProtoId<NivalisPerkPrototype> Id = default!;
    public string? Name;
    public string? Description;
}

[Serializable, NetSerializable]
public sealed class NivalisTraitDraftUiState : BoundUserInterfaceState
{
    public List<NivalisTraitDraftChoice> Choices = new();
}

[Serializable, NetSerializable]
public sealed class NivalisTraitDraftSelectedMessage(ProtoId<NivalisPerkPrototype> traitId, EntityUid actor) : BoundUserInterfaceMessage
{
    public ProtoId<NivalisPerkPrototype> TraitId = traitId;
    public EntityUid Actor = actor;
}

