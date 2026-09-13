using Robust.Shared.GameStates;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared._Nivalis.Perks;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true), AutoGenerateComponentPause]
public sealed partial class NivalisLazarusComponent : Component
{
    [DataField, AutoNetworkedField, ViewVariables(VVAccess.ReadWrite)]
    public float Charge;

    [DataField, AutoNetworkedField, ViewVariables(VVAccess.ReadWrite)]
    public int Shots = 2;

    [ViewVariables(VVAccess.ReadOnly)]
    public float RechargeRate = 100f / 70f;

    [ViewVariables(VVAccess.ReadOnly)]
    public int MaxShots = 2;

    [ViewVariables(VVAccess.ReadOnly)]
    public float ShotCost = 50f;

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoPausedField, ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan NextShotTime;

    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan ShotCooldown = TimeSpan.FromSeconds(0.4);

    [ViewVariables(VVAccess.ReadOnly)]
    public bool Initialised;
}
