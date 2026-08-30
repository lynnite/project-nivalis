using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._Nivalis.GameTicking.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class NivalisStormWaveComponent : Component
{
    [DataField]
    public List<NivalisStormWaveGroup> Waves = new();
}

[DataDefinition]
public sealed partial class NivalisStormWaveGroup
{
    [DataField]
    public List<NivalisStormWaveSpawn> Spawns = new();
}

[DataDefinition]
public sealed partial class NivalisStormWaveSpawn
{
    [DataField]
    public EntProtoId Prototype = "MobNivalisScavenger";

    [DataField]
    public int Count = 1;
}

