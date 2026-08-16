using Content.Shared.Roles;
using Robust.Shared.Prototypes;

namespace Content.Server._Nivalis.GameTicking.Rules.Components;

[RegisterComponent, Access(typeof(NivalisSurvivorRuleSystem))]
public sealed partial class NivalisSurvivorRuleComponent : Component
{
    [DataField]
    public EntProtoId SpawnMarker = "NivalisSurvivorSpawnPoint";

    [DataField]
    public EntProtoId MindRole = "MindRoleNivalisSurvivor";

    [DataField]
    public ProtoId<JobPrototype> Job = "NivalisSurvivor";

    [DataField]
    public ProtoId<StartingGearPrototype> Gear = "NivalisSurvivorGear";

    [DataField]
    public float EndCheckDelay = 1.0f;

    [DataField]
    public TimeSpan NextRoundEndCheck;

    [DataField]
    public TimeSpan RoundEndDelay = TimeSpan.FromSeconds(10);

    [DataField]
    public bool RoundEndTriggered;
}

