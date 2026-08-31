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
    public ProtoId<NivalisTraitPrototype> Id = default!;
    public string? Name;
    public string? Description;
}

[Serializable, NetSerializable]
public sealed class NivalisTraitDraftUiState : BoundUserInterfaceState
{
    public List<NivalisTraitDraftChoice> Choices = new();
}

[Serializable, NetSerializable]
public sealed class NivalisTraitDraftSelectedMessage(ProtoId<NivalisTraitPrototype> traitId) : BoundUserInterfaceMessage
{
    public ProtoId<NivalisTraitPrototype> TraitId = traitId;
}
