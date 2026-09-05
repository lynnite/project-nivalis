namespace Content.Server._Nivalis.Perks;

[RegisterComponent, Access(typeof(NivalisBlitzerSystem))]
public sealed partial class NivalisBlitzerComponent : Component
{
    [ViewVariables(VVAccess.ReadWrite)]
    public float Charge;

    [ViewVariables(VVAccess.ReadOnly)]
    public float RechargeRate = 1.1f;

    [ViewVariables(VVAccess.ReadOnly)]
    public float BombCost = 16.67f;

    [ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan NextActionAt = TimeSpan.Zero;

    [ViewVariables(VVAccess.ReadOnly)]
    public bool Initialised;

    public List<EntityUid> Bombs = new();
}
