using Robust.Shared.GameStates;

namespace Content.Shared._Nivalis.Weapons;

/// <summary>
///     Marks a gun as a "scatter gun" / shotgun-style weapon. Fires multiple
///     projectiles per shot spread over a configured number of degrees.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class NivalisScatterGunComponent : Component
{
    /// <summary>
    ///     How many projectiles are fired per pull of the trigger.
    /// </summary>
    [DataField, AutoNetworkedField]
    public int ProjectilesPerShot = 1;

    /// <summary>
    ///     The total spread, in degrees, that the projectiles are evenly distributed over.
    ///     A value of 0 means all projectiles fly dead straight.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float DegreesScatter = 0f;

    /// <summary>
    ///     Whether the scatter has been applied to the gun's spread fields yet.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool ScatterInitialized;
}

