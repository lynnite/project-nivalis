using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
namespace Content.Shared._Nivalis.GameTicking.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class NivalisStormSpawnerComponent : Component
{
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public EntProtoId SpawnPrototype = "MobNivalisScavenger";

    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public int SpawnCount = 1;

    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public int ThreatRampPerWave = 1;

    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public float SpawnJitter = 2f;
}

