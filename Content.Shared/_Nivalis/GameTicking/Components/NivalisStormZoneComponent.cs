using Content.Shared.Damage.Prototypes;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared._Nivalis.GameTicking.Components;

/// <summary>
///     Marks a map-able entity as a storm spawner. When the survival "Storm" phase
///     begins the spawner becomes an active storm cloud (a radius zone) that:
///     - Damages entities with <see cref="NivalisSurvivorComponent"/> inside it.
///     - Applies client-side storm visuals (view limiting, flying snow, distortion).
///     This is distinct from <see cref="NivalisStormSpawnerComponent"/>, which only
///     spawns hostile scavengers.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true), AutoGenerateComponentPause]
public sealed partial class NivalisStormZoneComponent : Component
{
    /// <summary>
    ///     Whether this storm cloud is currently active (a storm is ongoing).
    /// </summary>
    [DataField, AutoNetworkedField, ViewVariables(VVAccess.ReadWrite)]
    public bool Active;

    /// <summary>
    ///     Radius (meters) of the storm cloud's damaging/visual zone around the marker.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public float Radius = 12f;

    /// <summary>
    ///     Damage dealt to each survivor inside the storm every <see cref="DamageInterval"/>.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public float StormDamagePerSecond = 5f;

    /// <summary>
    ///     How often the storm ticks (seconds real-time).
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public float DamageInterval = 1f;

    /// <summary>
    ///     The damage type used by the storm. Kept as a configurable proto in case
    ///     future tuning wants a distinct type.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public ProtoId<DamageTypePrototype> DamageType = "Storm";

    /// <summary>
    ///     Next time the storm will tick damage.
    /// </summary>
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoPausedField]
    public TimeSpan NextTick = TimeSpan.Zero;
}

