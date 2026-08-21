using Content.Shared._Nivalis.Weapons;
using Content.Shared.Input;
using Robust.Client.Player;
using Robust.Shared.Input.Binding;

namespace Content.Client._Nivalis.Weapons;

/// <summary>
///     Binds the Nivalis aim key (right mouse button) to send aim input to the server.
/// </summary>
public sealed partial class NivalisAimSystem : SharedNivalisAimSystem
{
    [Dependency] private readonly IPlayerManager _player = default!;

    public override void Initialize()
    {
        base.Initialize();

        CommandBinds.Builder
            .Bind(ContentKeyFunctions.NivalisAim,
                InputCmdHandler.FromDelegate(
                    enabled: _ => OnAimChanged(true),
                    disabled: _ => OnAimChanged(false),
                    outsidePrediction: false))
            .Register<NivalisAimSystem>();
    }

    private void OnAimChanged(bool active)
    {
        if (_player.LocalEntity == null)
            return;

        RaisePredictiveEvent(new NivalisAimEvent(active));
    }
}
