using System.Numerics;
using Content.Shared._Nivalis.Perks;
using Content.Shared.Input;
using Robust.Client.Graphics;
using Robust.Client.Input;
using Robust.Client.Network;
using Robust.Client.Player;
using Robust.Shared.Input;
using Robust.Shared.Input.Binding;
using Robust.Shared.Map;
using Robust.Shared.Timing;

namespace Content.Client._Nivalis.Perks;

public sealed partial class NivalisPerkAbilitySystem : EntitySystem
{
    private const float TapCadence = 0.3f;

    [Dependency] private IEntityNetworkManager _net = default!;
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private IPlayerManager _player = default!;
    [Dependency] private IEyeManager _eyeManager = default!;
    [Dependency] private IInputManager _inputManager = default!;

    private TimeSpan _downStarted;
    private bool _isDown;

    public override void Initialize()
    {
        base.Initialize();

        CommandBinds.Builder
            .Bind(ContentKeyFunctions.NivalisPerkAbility,
                InputCmdHandler.FromDelegate(
                    enabled: _ => OnKeyDown(),
                    disabled: _ => OnKeyUp(),
                    outsidePrediction: false))
            .Register<NivalisPerkAbilitySystem>();
    }

    private void OnKeyDown()
    {
        if (_isDown)
            return;
        _isDown = true;
        _downStarted = _timing.CurTime;
    }

    private void OnKeyUp()
    {
        if (!_isDown)
            return;
        _isDown = false;

        var held = _timing.CurTime - _downStarted;
        SendAbility(held > TimeSpan.FromSeconds(TapCadence), GetAimDirection());
    }

    private Vector2 GetAimDirection()
    {
        var origin = _eyeManager.CurrentEye.Position.Position;
        var mouse = _eyeManager.ScreenToMap(_inputManager.MouseScreenPosition);
        if (mouse.MapId == MapId.Nullspace)
            return Vector2.Zero;

        var delta = mouse.Position - origin;
        return delta.LengthSquared() < 0.0001f ? Vector2.Zero : Vector2.Normalize(delta);
    }

    private void SendAbility(bool holding, Vector2 aimDirection)
    {
        _net.SendSystemNetworkMessage(new NivalisPerkAbilityPressedMessage(holding, aimDirection));
    }
}

