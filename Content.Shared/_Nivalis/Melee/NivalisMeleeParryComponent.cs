using Content.Shared.Damage;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared._Nivalis.Melee;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true), AutoGenerateComponentPause]
public sealed partial class NivalisMeleeParryComponent : Component
{
    [DataField, AutoNetworkedField]
    public float ParryCooldown = 1f;

    [DataField, AutoNetworkedField]
    public float ParryWindow = 1f;

    [DataField, AutoNetworkedField]
    public float StunDuration = 1.5f;

    [DataField, AutoNetworkedField]
    public float FailedParryPenalty = 0.5f;

    [AutoNetworkedField]
    public bool Protecting;

    [AutoNetworkedField]
    public bool ParriedThisStance;

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoNetworkedField, AutoPausedField]
    public TimeSpan NextParry;

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoNetworkedField, AutoPausedField]
    public TimeSpan ParryWindowEnd;

    [DataField]
    public SoundSpecifier? Sound;
}
public sealed class NivalisMeleeParryAttemptEvent : EntityEventArgs
{
    public EntityUid Victim;

    public EntityUid Attacker;

    public EntityUid Weapon;

    public bool Parried;

    public NivalisMeleeParryAttemptEvent(EntityUid victim, EntityUid attacker, EntityUid weapon)
    {
        Victim = victim;
        Attacker = attacker;
        Weapon = weapon;
    }
}

