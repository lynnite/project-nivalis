using System.Numerics;
using Robust.Shared.GameStates;

namespace Content.Shared._Nivalis.Perks;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true)]
public sealed partial class NivalisArbiterComponent : Component
{
    public bool Ready;

    public TimeSpan ReadyAt = TimeSpan.Zero;

    public Vector2 ShortAim = Vector2.Zero;

    public Vector2 LongAim = Vector2.Zero;

    public bool LongReady;

    public TimeSpan LongWindupAt = TimeSpan.Zero;

    public bool ReadyCueSent;

    public bool CycleResolved;

    [DataField, AutoNetworkedField, ViewVariables(VVAccess.ReadWrite)]
    public float Charge;

    [ViewVariables(VVAccess.ReadOnly)]
    public float RechargeRate = 10f;

    [ViewVariables(VVAccess.ReadOnly)]
    public float BlastCost = 100f;

    [ViewVariables(VVAccess.ReadOnly)]
    public bool Initialised;
}
