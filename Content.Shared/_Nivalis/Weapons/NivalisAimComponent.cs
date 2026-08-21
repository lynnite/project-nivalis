using Robust.Shared.GameStates;

namespace Content.Shared._Nivalis.Weapons;

/// <summary>
///     Marks a Nivalis gun as aimable. While the player holds the aim key (right mouse
///     button) the gun's <see cref="NivalisScatterDirectionComponent"/> values are
///     replaced with the reduced scatter values from <see cref="NivalisAimedScatterComponent"/>.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class NivalisAimComponent : Component
{
    /// <summary>
    ///     Whether the gun is currently being aimed (right mouse button held).
    ///     Updated on both client (for prediction/predicted scatter) and server.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField, AutoNetworkedField]
    public bool Aiming;
}
