using Content.Shared.Damage;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared._Nivalis.Status;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true), AutoGenerateComponentPause]
public sealed partial class NivalisSurvivalResourceComponent : Component
{
    [DataField, AutoNetworkedField, ViewVariables(VVAccess.ReadWrite)]
    public float Hunger = 100f;

    [DataField, AutoNetworkedField, ViewVariables(VVAccess.ReadWrite)]
    public float MaxHunger = 100f;

    [DataField, AutoNetworkedField, ViewVariables(VVAccess.ReadWrite)]
    public float HungerDecay = 0.3f;

    [DataField, AutoNetworkedField, ViewVariables(VVAccess.ReadWrite)]
    public float HungerCriticalFraction = 0.15f;

    [DataField, AutoNetworkedField, ViewVariables(VVAccess.ReadWrite)]
    public float Thirst = 100f;

    [DataField, AutoNetworkedField, ViewVariables(VVAccess.ReadWrite)]
    public float MaxThirst = 100f;

    [DataField, AutoNetworkedField, ViewVariables(VVAccess.ReadWrite)]
    public float ThirstDecay = 0.4f;

    [DataField, AutoNetworkedField, ViewVariables(VVAccess.ReadWrite)]
    public float ThirstCriticalFraction = 0.15f;

    [DataField]
    public DamageSpecifier StarvationDamage = new()
    {
        DamageDict = { ["Heat"] = 0f }, // disabled for now
    };

    [DataField]
    public DamageSpecifier DehydrationDamage = new()
    {
        DamageDict = { ["Heat"] = 1.5f },
    };

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoPausedField]
    public TimeSpan NextTick = TimeSpan.Zero;
}

