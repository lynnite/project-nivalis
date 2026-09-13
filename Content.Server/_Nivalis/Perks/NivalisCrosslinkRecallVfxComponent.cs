using Robust.Shared.Map;

namespace Content.Server._Nivalis.Perks;

[RegisterComponent, Access(typeof(NivalisCrosslinkSystem))]
public sealed partial class NivalisCrosslinkRecallVfxComponent : Component
{
    [ViewVariables(VVAccess.ReadWrite)]
    public MapCoordinates From;

    [ViewVariables(VVAccess.ReadWrite)]
    public MapCoordinates To;

    [ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan StartTime;

    [ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan SegmentInterval = TimeSpan.FromSeconds(0.15f);

    [ViewVariables(VVAccess.ReadWrite)]
    public int SegmentCount;

    [ViewVariables(VVAccess.ReadWrite)]
    public int Revealed;

    [ViewVariables(VVAccess.ReadOnly)]
    public readonly List<EntityUid> Segments = new();
}
