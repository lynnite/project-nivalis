using System.Numerics;
using Content.Shared._Nivalis.Perks;
using Content.Shared.Input;
using Robust.Client.GameObjects;
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

    private const float CrosslinkHoldTime = 0.4f;

    [Dependency] private IEntityNetworkManager _net = default!;
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private IPlayerManager _player = default!;
    [Dependency] private IEyeManager _eyeManager = default!;
    [Dependency] private IInputManager _inputManager = default!;
    [Dependency] private MapSystem _map = default!;
    [Dependency] private SharedTransformSystem _transform = default!;

    private TimeSpan _downStarted;
    private bool _isDown;
    private bool _crosslinkDown;
    private bool _crosslinkRecalled;

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

    private bool IsCrosslink()
    {
        return _player.LocalEntity is { } local
            && TryComp<NivalisPerkComponent>(local, out var perk)
            && perk.Perk?.Id == "Crosslink";
    }

    private void OnKeyDown()
    {
        if (_isDown)
            return;
        _isDown = true;
        _downStarted = _timing.CurTime;
        _crosslinkDown = IsCrosslink();
        _crosslinkRecalled = false;
    }

    private void OnKeyUp()
    {
        if (!_isDown)
            return;
        _isDown = false;

        var held = _timing.CurTime - _downStarted;

        if (_crosslinkDown)
        {
            _crosslinkDown = false;
            if (!_crosslinkRecalled)
                SendCrosslinkAction(NivalisCrosslinkAction.PlaceKnife);
            return;
        }

        SendAbility(held > TimeSpan.FromSeconds(TapCadence), GetAimDirection());
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (!_isDown || !_crosslinkDown || _crosslinkRecalled)
            return;

        if (_timing.CurTime - _downStarted < TimeSpan.FromSeconds(CrosslinkHoldTime))
            return;

        _crosslinkRecalled = true;
        SendCrosslinkAction(NivalisCrosslinkAction.RecallKnife);
    }

    private void SendCrosslinkAction(NivalisCrosslinkAction action)
    {
        if (_player.LocalEntity is not { } local)
            return;

        var aim = GetAimDirection();

        EntityCoordinates coordinates;
        var mousePos = _eyeManager.PixelToMap(_inputManager.MouseScreenPosition);
        if (mousePos.MapId == MapId.Nullspace)
            coordinates = Transform(local).Coordinates;
        else if (_map.TryFindGridAt(mousePos, out var grid, out _))
            coordinates = _transform.ToCoordinates(grid, mousePos);
        else
            coordinates = _transform.ToCoordinates(_map.GetMap(mousePos.MapId), mousePos);

        _net.SendSystemNetworkMessage(new NivalisCrosslinkActionMessage(action, GetNetCoordinates(coordinates), aim));
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

