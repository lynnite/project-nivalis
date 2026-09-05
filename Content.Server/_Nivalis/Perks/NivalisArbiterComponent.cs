using System.Numerics;

namespace Content.Server._Nivalis.Perks;

/// <summary>
///     Transient server-side state for the Arbiter "Knuckleboom" wrist shotshell cannons.
///
///     A first F press readies a shell. A second F press, while that shell is still readied
///     (before it auto-resolves), upgrades it to a high, far-reaching "longshot". If the shell
///     is left alone it resolves as a short-distance "shortshot". A yellow screen flash cues
///     the player that the readied shell is about to auto-fire and that they may upgrade it.
/// </summary>
[RegisterComponent, Access(typeof(NivalisArbiterSystem))]
public sealed partial class NivalisArbiterComponent : Component
{
    /// <summary>True while a shell is readied and waiting to auto-resolve or be upgraded.</summary>
    public bool Ready;

    /// <summary>When the readied shell was first pressed.</summary>
    public TimeSpan ReadyAt = TimeSpan.Zero;

    /// <summary>Aim direction for the readied shortshot.</summary>
    public Vector2 ShortAim = Vector2.Zero;

    /// <summary>Aim direction for an upgraded longshot.</summary>
    public Vector2 LongAim = Vector2.Zero;

    /// <summary>True once a longshot is armed and winding up.</summary>
    public bool LongReady;

    /// <summary>When the longshot wind-up began.</summary>
    public TimeSpan LongWindupAt = TimeSpan.Zero;

    /// <summary>Whether the yellow "press again" cue has been shown for this readied shell.</summary>
    public bool ReadyCueSent;

    /// <summary>True once the current readied shell has been resolved (fired as short or long).</summary>
    public bool CycleResolved;

    // --- Charge / cooldown model (a 0-100% counter, mirroring NivalisBlitzerComponent) ---

    /// <summary>Current charge as a percentage (0-100). A blast requires full charge and empties it.</summary>
    [ViewVariables(VVAccess.ReadWrite)]
    public float Charge;

    /// <summary>Charge gained per second; 10/s -> the full 100% bar replenishes over 10 seconds.</summary>
    [ViewVariables(VVAccess.ReadOnly)]
    public float RechargeRate = 10f;

    /// <summary>Charge consumed by one knuckle blast (the whole bar: 100% per shot).</summary>
    [ViewVariables(VVAccess.ReadOnly)]
    public float BlastCost = 100f;

    [ViewVariables(VVAccess.ReadOnly)]
    public bool Initialised;
}
