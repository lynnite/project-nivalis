using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
namespace Content.Shared._Nivalis.Status;

[RegisterComponent, NetworkedComponent]
public sealed partial class NivalisBleedComponent : Component
{
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public float DamagePerSecond = 1f;

    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public float Duration = 20f;
}
