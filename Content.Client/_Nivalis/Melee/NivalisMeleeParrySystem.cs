using Content.Shared._Nivalis.Melee;
using Content.Shared._Nivalis.Melee.Events;
using Content.Shared.Input;
using Robust.Client.Player;
using Robust.Shared.Input;
using Robust.Shared.Input.Binding;

namespace Content.Client._Nivalis.Melee;

public sealed partial class NivalisMeleeParrySystem : SharedNivalisMeleeParrySystem
{
    [Dependency] private readonly IPlayerManager _player = default!;

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
        if (_player.LocalEntity == null)
            return;

        RaisePredictiveEvent(new NivalisMeleeParryEvent(true));
    }
}
