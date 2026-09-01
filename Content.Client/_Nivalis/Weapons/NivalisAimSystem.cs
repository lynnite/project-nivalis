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

/// <summary>
///     Binds the Nivalis aim key (right mouse button) to send aim input to the server.
///     While aiming down sights, the camera leans slightly into the direction the player is
///     aiming/facing (a small directed zoom rather than a symmetric one). The slight zoom
///     multiplicatively stacks with exhaustion zoom (see <see cref="NivalisStaminaEffectsSystem"/>).
/// </summary>
public sealed partial class NivalisAimSystem : SharedNivalisAimSystem
{
    [Dependency] private IPlayerManager _player = default!;
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private IEyeManager _eyeManager = default!;
    [Dependency] private IInputManager _input = default!;
    [Dependency] private SharedHandsSystem _hands = default!;
    [Dependency] private NivalisStaminaEffectsSystem _stamina = default!;

    private bool _localAiming;
    private float _adsAmount;          // 0..1, how far into ADS we currently are (smoothed).
    private float _currentEyeZoom = 1.0f;

    /// <summary>
    ///     How far (world units) the camera leans toward the aim direction at full ADS.
    /// </summary>
    private const float MaxAdsOffset = 0.6f;

    /// <summary>The slight symmetric zoom-in applied while ADSing.</summary>
    private const float AdsMinZoomScale = 0.95f;

    private const float ZoomLerpSpeed = 8f;

    public override void Initialize()
    {
        base.Initialize();

        // Run after the stamina effects system so the ADS + exhaustion zoom stack correctly.
        UpdatesAfter.Add(typeof(NivalisStaminaEffectsSystem));

        // Guard against duplicate (EyeComponent, GetEyeOffsetEvent) subscriptions: the stamina
        // system already owns that event, so contribute our directed offset via the relayed event.
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

        // Reset local aim state if we no longer hold an aimable gun (e.g. swapped hands while ADS).
        if (_player.LocalEntity is { } owner)
        {
            var held = _hands.GetActiveItem((owner, null));
            if (_localAiming && (held == null || !HasComp<NivalisAimComponent>(held.Value)))
                _localAiming = false;
        }

        // Smoothly ramp the ADS amount up/down. This drives both the directed camera offset
        // (see OnGetEyeOffset) and the slight zoom below.
        var target = _localAiming ? 1f : 0f;
        _adsAmount += (target - _adsAmount) * MathF.Min(1f, frameTime * ZoomLerpSpeed);
        if (!_localAiming && MathF.Abs(_adsAmount) < 0.001f)
            _adsAmount = 0f;

        if (_player.LocalEntity is not { } pid)
            return;

        // Only drive the zoom while ADS is contributing (including smoothly winding down from it).
        if (_adsAmount <= 0f)
        {
            _currentEyeZoom = _eyeManager.CurrentEye.Zoom.X;
            return;
        }

        // Slight zoom-in that stacks multiplicatively with the exhaustion zoom scale.
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

        // Lean the camera toward where the player is aiming/facing while ADSing.
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

        // Only start/stop aiming if the active hand actually holds an aimable gun.
        var held = _hands.GetActiveItem((pid, null));
        if (held == null || !HasComp<NivalisAimComponent>(held.Value))
        {
            _localAiming = false;
            return;
        }

        // Track the local aim state so the camera response is always immediate, even outside
        // the first predicted tick. Only the network event is prediction-gated.
        _localAiming = active;

        if (!_timing.IsFirstTimePredicted)
            return;

        RaisePredictiveEvent(new NivalisAimEvent(active));
    }
}
