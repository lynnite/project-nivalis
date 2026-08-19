using Robust.Shared.GameStates;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared._Nivalis.Status;

/// <summary>
///     Tracks an active Nivalis bleed on a mob. Added while the corresponding
///     status effect is active and drives the per-second brute ticking.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentPause]
public sealed partial class NivalisBleedActiveComponent : Component
{
    /// <summary>
    ///     Brute damage applied per second.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public float DamagePerSecond = 1f;

    /// <summary>
    ///     The entity that caused the bleeding, used for kill attribution.
    /// </summary>
    [DataField]
    public EntityUid? Source;

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoPausedField]
    public TimeSpan NextTick;
}
