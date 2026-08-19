using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Server._Nivalis.NPC;

/// <summary>
///     Marks a NPC as using Nivalis melee combat (weapons with
///     <see cref="Content.Shared._Nivalis.Melee.NivalisMeleeComponent"/>). The
///     <see cref="NivalisNPCCombatSystem"/> uses this to drive chained light attacks,
///     occasional heavy attacks and, for capable enemies, parrying.
/// </summary>
[RegisterComponent]
public sealed partial class NivalisMeleeCombatComponent : Component
{
    /// <summary>
    ///     Target to attack.
    /// </summary>
    [DataField]
    public EntityUid Target;

    /// <summary>
    ///     Roughly how often (seconds) the entity should attempt a heavy attack instead of a light one.
    ///     <c>0</c> disables heavy attacks entirely.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public float HeavyChance = 0f;

    /// <summary>
    ///     Whether this NPC is capable of parrying.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public bool CanParry;

    /// <summary>
    ///     When the NPC last entered parry state.
    /// </summary>
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    public TimeSpan LastParry;

    /// <summary>
    ///     How long the NPC should remain parrying for.
    /// </summary>
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    public TimeSpan ParryUntil;

    /// <summary>
    ///     Cooldown until the NPC may parry again.
    /// </summary>
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    public TimeSpan ParriedCooldownUntil;

    /// <summary>
    ///     Cooldown between parries.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public float ParryCooldown = 2f;
    /// <summary>
    ///     Current attack status, for HTN `Update` bookkeeping.
    /// </summary>
    [DataField]
    public NivalisCombatStatus Status = NivalisCombatStatus.Normal;
}

/// <summary>
///     Mirrors <see cref="Content.Server.NPC.Components.CombatStatus"/> for Nivalis combat.
/// </summary>
public enum NivalisCombatStatus : byte
{
    Normal,
    TargetOutOfRange,
    TargetUnreachable,
    NoWeapon,
    Alerted,
    TargetCrit,
    TargetLunging,
}

