using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Server._Nivalis.NPC;

[RegisterComponent]
public sealed partial class NivalisMeleeCombatComponent : Component
{
    [DataField]
    public EntityUid Target;

    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public float HeavyChance = 0f;

    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public bool CanParry;

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    public TimeSpan LastParry;

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    public TimeSpan ParryUntil;

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    public TimeSpan ParriedCooldownUntil;

    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public float ParryCooldown = 2f;
    [DataField]
    public NivalisCombatStatus Status = NivalisCombatStatus.Normal;
}

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

