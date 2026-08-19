using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared._Nivalis.Weapons;

/// <summary>
///     Marks a <c>BasicEntityAmmoProvider</c> gun that reloads its entire magazine on its own:
///     when the gun runs dry it waits <see cref="ReloadDelay"/> seconds (playing the magazine
///     eject sound) then fills the whole magazine back up (playing the magazine insert sound).
///     Used by scavenger gunners so they never need external ammo and never drop their gun.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, AutoGenerateComponentPause]
public sealed partial class NivalisAutoReloadComponent : Component
{
    /// <summary>
    ///     How long the gun waits after running empty before its magazine is refilled.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float ReloadDelay = 2f;

    [DataField, AutoNetworkedField]
    public SoundSpecifier? SoundMagOut;

    [DataField, AutoNetworkedField]
    public SoundSpecifier? SoundMagIn;

    /// <summary>
    ///     Set on the server while a reload is in progress.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    public bool Reloading;

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoNetworkedField, AutoPausedField]
    public TimeSpan NextReload;
}
