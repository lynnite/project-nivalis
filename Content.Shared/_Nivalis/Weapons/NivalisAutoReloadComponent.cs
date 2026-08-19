using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared._Nivalis.Weapons;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, AutoGenerateComponentPause]
public sealed partial class NivalisAutoReloadComponent : Component
{
    [DataField, AutoNetworkedField]
    public float ReloadDelay = 2f;

    [DataField, AutoNetworkedField]
    public SoundSpecifier? SoundMagOut;

    [DataField, AutoNetworkedField]
    public SoundSpecifier? SoundMagIn;

    [ViewVariables(VVAccess.ReadWrite)]
    public bool Reloading;

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoNetworkedField, AutoPausedField]
    public TimeSpan NextReload;
}

