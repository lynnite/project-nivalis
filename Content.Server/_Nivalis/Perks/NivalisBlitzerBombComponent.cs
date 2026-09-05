namespace Content.Server._Nivalis.Perks;

[RegisterComponent, Access(typeof(NivalisBlitzerSystem))]
public sealed partial class NivalisBlitzerBombComponent : Component
{
    [ViewVariables(VVAccess.ReadOnly)]
    public float DespawnAfter = 165f;

    [ViewVariables(VVAccess.ReadWrite)]
    public bool Detonated;
}
