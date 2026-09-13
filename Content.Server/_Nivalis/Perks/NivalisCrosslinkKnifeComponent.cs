using Robust.Shared.GameObjects;

namespace Content.Server._Nivalis.Perks;

[RegisterComponent, Access(typeof(NivalisCrosslinkSystem))]
public sealed partial class NivalisCrosslinkKnifeComponent : Component
{
    [ViewVariables(VVAccess.ReadWrite)]
    [Access(typeof(NivalisCrosslinkSystem), Other = AccessPermissions.ReadWrite)]
    public EntityUid? OwnerPlayer;

    [ViewVariables(VVAccess.ReadWrite)]
    [Access(typeof(NivalisCrosslinkSystem), Other = AccessPermissions.ReadWrite)]
    public bool Planted;

    [ViewVariables(VVAccess.ReadOnly)]
    [Access(typeof(NivalisCrosslinkSystem), Other = AccessPermissions.ReadWrite)]
    public readonly List<EntityUid> Wires = new();
}

