using Content.Shared.Damage;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared._Nivalis.Melee;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(fieldDeltas: true), AutoGenerateComponentPause]
public sealed partial class NivalisMeleeComponent : Component
{
    [DataField, AutoNetworkedField]
    public float LightStaminaDamage = 5f;

    [DataField, AutoNetworkedField]
    public float HeavyStaminaDamage = 15f;

    [DataField, AutoNetworkedField]
    public DamageSpecifier LightDamage = new();

    [DataField, AutoNetworkedField]
    public DamageSpecifier HeavyDamage = new();

    [DataField, AutoNetworkedField]
    public int LightComboCount = 3;

    [DataField, AutoNetworkedField]
    public float LightComboInterval = 0.13f;

    [DataField, AutoNetworkedField]
    public float LightComboRecovery = 0.5f;

    [DataField, AutoNetworkedField]
    public int ComboHits;

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoNetworkedField]
    public TimeSpan NextAttack;

    [DataField, AutoNetworkedField]
    public bool AutoAttack;
    [AutoNetworkedField]
    public bool Attacking;
    [DataField, AutoNetworkedField]
    public float AttackRate = 1f;
    [DataField, AutoNetworkedField]
    public float Range = 1.5f;
    [DataField, AutoNetworkedField]
    public Angle Angle = Angle.FromDegrees(60);
    [DataField, AutoNetworkedField]
    public EntProtoId Animation = "WeaponArcThrust";
    [DataField, AutoNetworkedField]
    public EntProtoId WideAnimation = "WeaponArcSlash";
    [DataField, AutoNetworkedField]
    public Angle WideAnimationRotation = Angle.Zero;
    [DataField, AutoNetworkedField]
    public bool SwingLeft;

    [DataField, AutoNetworkedField]
    public float AnimationOffset = 1f;
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("soundSwing"), AutoNetworkedField]
    public SoundSpecifier SwingSound { get; set; } = new SoundPathSpecifier("/Audio/Weapons/punchmiss.ogg")
    {
        Params = AudioParams.Default.AddVolume(-3f).WithVariation(0.025f),
    };
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("soundHit"), AutoNetworkedField]
    public SoundSpecifier? HitSound;

    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("soundNoDamage"), AutoNetworkedField]
    public SoundSpecifier NoDamageSound { get; set; } = new SoundCollectionSpecifier("WeakHit");
}

