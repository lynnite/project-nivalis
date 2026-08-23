using Content.Shared._Nivalis.Melee;
using Content.Shared._Nivalis.Melee.Events;
using Content.Shared.Input;
using Robust.Client.Player;
using Robust.Shared.Input;
using Robust.Shared.Input.Binding;
using Robust.Shared.Timing;

namespace Content.Client._Nivalis.Melee;

public sealed partial class NivalisMeleeParrySystem : SharedNivalisMeleeParrySystem
{
    [Dependency] private IPlayerManager _player = default!;
    [Dependency] private IGameTiming _timing = default!;

    public override void Initialize()
    {
        base.Initialize();

        CommandBinds.Builder
            .Bind(ContentKeyFunctions.NivalisParry,
                InputCmdHandler.FromDelegate(
                    enabled: _ => OnParryPressed(),
                    outsidePrediction: false))
            .Register<NivalisMeleeParrySystem>();
    }

    private void OnParryPressed()
    {
        if (!_timing.IsFirstTimePredicted || _player.LocalEntity == null)
            return;

        RaisePredictiveEvent(new NivalisMeleeParryEvent(true));
    }
}
