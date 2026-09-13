namespace Content.Server._Nivalis.Perks;

[RegisterComponent, Access(typeof(NivalisCrosslinkSystem))]
public sealed partial class NivalisCrosslinkWireComponent : Component
{
    [ViewVariables(VVAccess.ReadWrite)]
    public EntityUid? KnifeA;

    [ViewVariables(VVAccess.ReadWrite)]
    public EntityUid? KnifeB;

    [ViewVariables(VVAccess.ReadWrite)]
    public EntityUid? Owner;

    [ViewVariables(VVAccess.ReadOnly)]
    public readonly Dictionary<EntityUid, TimeSpan> SnaredTargets = new();

    public bool IsSnareOnCooldown(EntityUid target)
    {
        return SnaredTargets.ContainsKey(target);
    }
}
