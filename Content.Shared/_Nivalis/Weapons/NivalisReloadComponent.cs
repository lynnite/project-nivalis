using Robust.Shared.GameStates;

namespace Content.Shared._Nivalis.Weapons;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class NivalisReloadComponent : Component
{
    [DataField, AutoNetworkedField]
    public float ReloadDelay = 2f;

    [DataField, AutoNetworkedField]
    public bool BreakOnMove;

    [DataField, AutoNetworkedField]
    public float WalkSpeedMultiplier = 0.6f;

    [DataField, AutoNetworkedField]
    public float SprintSpeedMultiplier = 0.6f;
}
