using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._Nivalis.Perks;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true)]
public sealed partial class NivalisExecutionerComponent : Component
{
    [DataField, AutoNetworkedField, ViewVariables(VVAccess.ReadWrite)]
    public float Durability = 100f;

    public float MaxDurability = 100f;

    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public float RegenPerSecond = 0.5f;

    [DataField, AutoNetworkedField, ViewVariables(VVAccess.ReadWrite)]
    public bool Live;

    [DataField, AutoNetworkedField, ViewVariables(VVAccess.ReadWrite)]
    public bool Broken;

    [DataField, AutoNetworkedField, ViewVariables(VVAccess.ReadWrite)]
    public int BountyCount;

    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public float DamageThreshold = 2f;

    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public float AbsorbPerHit = 4f;

    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public float RequipThreshold = 50f;

    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public float ScrapPerBounty = 7f;

    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public float MeleeDamagePerBounty = 0.10f;
}

