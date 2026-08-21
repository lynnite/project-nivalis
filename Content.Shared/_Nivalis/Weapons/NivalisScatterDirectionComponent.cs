using Robust.Shared.GameStates;

namespace Content.Shared._Nivalis.Weapons;

/// <summary>
///     Determines the directional range (to the left and right, in degrees) that a
///     projectile spreads across when fired from a Nivalis scatter gun.
///     A value of 0 on every field means the projectile flies dead on to the cursor.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class NivalisScatterDirectionComponent : Component
{
    /// <summary>Leftmost scatter limit, in degrees (how far it may go to the left).</summary>
    [DataField, AutoNetworkedField]
    public float MinLeft;

    /// <summary>Rightmost scatter limit, in degrees (how far it may go to the right).</summary>
    [DataField, AutoNetworkedField]
    public float MaxLeft;

    /// <summary>Leftmost scatter limit on the right hand side, in degrees.</summary>
    [DataField, AutoNetworkedField]
    public float MinRight;

    /// <summary>Rightmost scatter limit on the right hand side, in degrees.</summary>
    [DataField, AutoNetworkedField]
    public float MaxRight;
}
