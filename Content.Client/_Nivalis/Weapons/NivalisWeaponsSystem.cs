using Content.Shared._Nivalis.Weapons;
using Content.Shared.Input;
using Robust.Client.Player;
using Robust.Shared.Input.Binding;
using Robust.Shared.Timing;

namespace Content.Client._Nivalis.Weapons;

public sealed partial class NivalisWeaponsSystem : SharedNivalisWeaponsSystem
{
    [Dependency] private readonly IPlayerManager _player = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    public override void Initialize()
    {
        base.Initialize();

        CommandBinds.Builder
            .Bind(ContentKeyFunctions.NivalisReload,
                InputCmdHandler.FromDelegate(
                    enabled: _ => OnReloadKey(true),
                    disabled: _ => OnReloadKey(false),
                    outsidePrediction: false))
            .Bind(ContentKeyFunctions.NivalisUnload,
                InputCmdHandler.FromDelegate(
                    enabled: _ => OnUnloadKey(true),
                    disabled: _ => OnUnloadKey(false),
                    outsidePrediction: false))
            .Register<NivalisWeaponsSystem>();
    }

    private void OnReloadKey(bool active)
    {
        if (!active || !_timing.IsFirstTimePredicted || _player.LocalEntity == null)
            return;

        RaisePredictiveEvent(new NivalisReloadEvent(true));
    }

    private void OnUnloadKey(bool active)
    {
        if (!active || !_timing.IsFirstTimePredicted || _player.LocalEntity == null)
            return;

        RaisePredictiveEvent(new NivalisUnloadEvent());
    }
}
