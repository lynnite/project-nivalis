using Content.Shared.Damage;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;

namespace Content.Shared._Nivalis.Weapons;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class NivalisDecimatorComponent : Component
{
    [DataField, AutoNetworkedField]
    public DamageSpecifier ExplosionDamage = new();

    [DataField, AutoNetworkedField]
    public float ExplosionRadius = 1.5f;

    [DataField, AutoNetworkedField]
    public float ExplosionEdgeMultiplier = 0.25f;

    [DataField, AutoNetworkedField]
    public SoundSpecifier ExplosionSound = new SoundPathSpecifier("/Audio/Effects/explosion_small1.ogg")
    {
        Params = AudioParams.Default.AddVolume(-6f).WithVariation(0.1f),
    };

    [DataField, AutoNetworkedField]
    public float AttackSpeedGrowth = 0.5f;

    [DataField, AutoNetworkedField]
    public float MaxAttackSpeedMultiplier = 3f;

    [DataField]
    public float BaseAttackRate = 0f;

    [ViewVariables(VVAccess.ReadWrite)]
    public float SpeedMultiplier = 1f;
}
