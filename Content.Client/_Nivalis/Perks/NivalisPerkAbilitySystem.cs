using Content.Shared._Nivalis.Perks;
using Content.Shared.Input;
using Robust.Client.Network;
using Robust.Client.Player;
using Robust.Shared.Input;
using Robust.Shared.Input.Binding;

namespace Content.Client._Nivalis.Perks;

/// <summary>
///     Client-side hook for the perk ability key (F). Forwards a press to the server, which
///     is authoritative over the equipped perk's cooldown and effect dispatch.
/// </summary>
public sealed class NivalisPerkAbilitySystem : EntitySystem
{
    [Dependency] private IEntityNetworkManager _net = default!;

    public override void Initialize()
    {
        base.Initialize();

        CommandBinds.Builder
            .Bind(ContentKeyFunctions.NivalisPerkAbility,
                InputCmdHandler.FromDelegate(
                    enabled: _ => OnAbilityPressed(),
                    outsidePrediction: false))
            .Register<NivalisPerkAbilitySystem>();
    }

    private void OnAbilityPressed()
    {
        _net.SendSystemNetworkMessage(new NivalisPerkAbilityPressedMessage());
    }
}

