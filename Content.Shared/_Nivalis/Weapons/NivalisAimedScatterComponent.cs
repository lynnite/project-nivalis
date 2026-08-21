using Robust.Shared.GameStates;

namespace Content.Shared._Nivalis.Weapons;

/// <summary>
///     The reduced scatter values applied while a <see cref="NivalisAimComponent"/> gun
///     is being aimed (right mouse button held). The firing system will use these in place
///     of the normal <see cref="NivalisScatterDirectionComponent"/> values while aiming.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class NivalisAimedScatterComponent : Component
{
    [DataField, AutoNetworkedField]
    public float MinLeft;

    [DataField, AutoNetworkedField]
    public float MaxLeft;

    [DataField, AutoNetworkedField]
    public float MinRight;

    [DataField, AutoNetworkedField]
    public float MaxRight;
}
