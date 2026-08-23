using Content.Shared._Nivalis.Weapons;
using Content.Shared.Input;
using Robust.Client.Player;
using Robust.Shared.Input.Binding;
using Robust.Shared.Timing;

namespace Content.Client._Nivalis.Weapons;

/// <summary>
///     Binds the Nivalis aim key (right mouse button) to send aim input to the server.
/// </summary>
public sealed partial class NivalisAimSystem : SharedNivalisAimSystem
{
    [Dependency] private IPlayerManager _player = default!;
    [Dependency] private IGameTiming _timing = default!;

    public override void Initialize()
    {
        base.Initialize();

        CommandBinds.Builder
            .Bind(ContentKeyFunctions.NivalisAim,
                InputCmdHandler.FromDelegate(
                    enabled: _ => OnAimChanged(true),
                    disabled: _ => OnAimChanged(false),
                    handle: false,
                    outsidePrediction: false))
            .Register<NivalisAimSystem>();
    }

    private void OnAimChanged(bool active)
    {
        if (!_timing.IsFirstTimePredicted || _player.LocalEntity == null)
            return;

        RaisePredictiveEvent(new NivalisAimEvent(active));
    }
}
