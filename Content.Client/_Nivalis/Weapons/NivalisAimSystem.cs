using System.Numerics;
using Content.Client._Nivalis.Stamina;
using Content.Shared._Nivalis.Weapons;
using Content.Shared.Camera;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Input;
using Content.Shared.Movement.Components;
using Content.Shared.Movement.Systems;
using Robust.Client.Graphics;
using Robust.Client.Input;
using Robust.Client.Player;
using Robust.Shared.Input.Binding;
using Robust.Shared.Map;
using Robust.Shared.Timing;

namespace Content.Client._Nivalis.Weapons;

public sealed partial class NivalisAimSystem : SharedNivalisAimSystem
{
    [Dependency] private IPlayerManager _player = default!;
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private IEyeManager _eyeManager = default!;
    [Dependency] private IInputManager _input = default!;
    [Dependency] private SharedHandsSystem _hands = default!;
    [Dependency] private NivalisStaminaEffectsSystem _stamina = default!;

    private bool _localAiming;
    private float _adsAmount;
    private float _currentEyeZoom = 1.0f;

    private const float MaxAdsOffset = 0.6f;

    private const float AdsMinZoomScale = 0.95f;

    private const float ZoomLerpSpeed = 8f;

    public override void Initialize()
    {
        base.Initialize();

        UpdatesAfter.Add(typeof(NivalisStaminaEffectsSystem));

        SubscribeLocalEvent<EyeComponent, GetEyeOffsetRelayedEvent>(OnGetEyeOffset);

        CommandBinds.Builder
            .Bind(ContentKeyFunctions.NivalisAim,
                InputCmdHandler.FromDelegate(
                    enabled: _ => OnAimChanged(true),
                    disabled: _ => OnAimChanged(false),
                    handle: false,
                    outsidePrediction: false))
            .Register<NivalisAimSystem>();
    }

    public override void FrameUpdate(float frameTime)
    {
        base.FrameUpdate(frameTime);

        if (_player.LocalEntity is { } owner)
        {
            var held = _hands.GetActiveItem((owner, null));
            if (_localAiming && (held == null || !HasComp<NivalisAimComponent>(held.Value)))
                _localAiming = false;
        }

        var target = _localAiming ? 1f : 0f;
        _adsAmount += (target - _adsAmount) * MathF.Min(1f, frameTime * ZoomLerpSpeed);
        if (!_localAiming && MathF.Abs(_adsAmount) < 0.001f)
            _adsAmount = 0f;

        if (_player.LocalEntity is not { } pid)
            return;

        if (_adsAmount <= 0f)
        {
            _currentEyeZoom = _eyeManager.CurrentEye.Zoom.X;
            return;
        }

        var zoomScale = _stamina.CurrentExhaustionZoomScale * (1f + (AdsMinZoomScale - 1f) * _adsAmount);

        var baseZoom = 1.0f;
        if (TryComp<ContentEyeComponent>(pid, out var contentEye))
            baseZoom = Math.Max(SharedContentEyeSystem.MinZoom.X, contentEye.TargetZoom.X);

        var targetZoom = baseZoom / zoomScale;

        _currentEyeZoom += (targetZoom - _currentEyeZoom) * MathF.Min(1f, frameTime * ZoomLerpSpeed);
        _currentEyeZoom = Math.Clamp(_currentEyeZoom, 0.1f, 2f);
        _eyeManager.CurrentEye.Zoom = new Vector2(_currentEyeZoom, _currentEyeZoom);
    }

    private void OnGetEyeOffset(Entity<EyeComponent> ent, ref GetEyeOffsetRelayedEvent args)
    {
        if (ent.Owner != _player.LocalEntity)
            return;

        if (_adsAmount <= 0f)
            return;

        if (_player.LocalEntity is not { } pid)
            return;

        var playerPos = _eyeManager.CurrentEye.Position.Position;
        var mousePos = _eyeManager.ScreenToMap(_input.MouseScreenPosition);
        if (mousePos.MapId == MapId.Nullspace)
            return;

        var aimDir = mousePos.Position - playerPos;
        if (aimDir.LengthSquared() < 0.0001f)
            return;

        args.Offset += aimDir.Normalized() * (MaxAdsOffset * _adsAmount);
    }

    private void OnAimChanged(bool active)
    {
        if (_player.LocalEntity is not { } pid)
        {
            _localAiming = false;
            return;
        }

        var held = _hands.GetActiveItem((pid, null));
        if (held == null || !HasComp<NivalisAimComponent>(held.Value))
        {
            _localAiming = false;
            return;
        }

        _localAiming = active;

        if (!_timing.IsFirstTimePredicted)
            return;

        RaisePredictiveEvent(new NivalisAimEvent(active));
    }
}
