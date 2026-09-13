using Robust.Shared.GameStates;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared._Nivalis.Perks;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true), AutoGenerateComponentPause]
public sealed partial class NivalisVagabondComponent : Component
{
    [DataField, AutoNetworkedField, ViewVariables(VVAccess.ReadWrite)]
    public int DogTags;

    [DataField, AutoNetworkedField, ViewVariables(VVAccess.ReadWrite)]
    public float AbilityPercent = 100f;

    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public float AbilityRechargeRate = 20f;

    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public int MaxDogTags = 10;

    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public int FullMeterTagThreshold = 9;

    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan SwingCooldown = TimeSpan.FromSeconds(1.5);

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoPausedField, ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan NextSwingTime;

    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public float MinSwingDamage = 10f;

    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public float MaxSwingDamage = 75f;

    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public float HealPerTag = 5f;

    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public float BurstDamageThreshold = 20f;

    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan BurstWindow = TimeSpan.FromSeconds(1.5);

    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public float ChipDamageTagChance = 0.5f;
}

