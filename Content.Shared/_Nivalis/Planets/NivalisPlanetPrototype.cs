using Content.Shared.Maps;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Shared._Nivalis.Planets;

[Prototype("nivalisPlanet")]
public sealed partial class NivalisPlanetPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    [DataField(required: true)]
    public LocId Name = default!;

    [DataField(required: true)]
    public ResPath MapFile = default!;

    [DataField(required: true)]
    public ProtoId<GameMapPrototype> MapId = default!;

    [DataField(required: true)]
    public EntProtoId PlanetEntity = default!;
}

