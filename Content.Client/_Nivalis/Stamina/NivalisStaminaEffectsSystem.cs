using System.Numerics;
using Content.Shared._Nivalis.Stamina;
using Content.Shared.Camera;
using Content.Shared.Movement.Components;
using Content.Shared.Movement.Systems;
using Content.Client.Eye;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Shared.GameObjects;
using Robust.Shared.Timing;

namespace Content.Client._Nivalis.Stamina;

public sealed partial class NivalisStaminaEffectsSystem : EntitySystem
{
    [Dependency] private IEyeManager _eyeManager = default!;
    [Dependency] private IPlayerManager _player = default!;
    [Dependency] private IOverlayManager _overlay = default!;
    [Dependency] private IGameTiming _timing = default!;
    private NivalisStaminaHeartbeatOverlay? _heartbeatOverlay;
    private float _currentZoom = 1.0f;

    /// <summary>
    ///     The exhaustion zoom scale factor currently being applied to the local player's eye.
    ///     &gt;= 1.0 (zoom out) as stamina depletes / exhaustion builds. Defaults to 1.0 when the
    ///     player has no <see cref="NivalisStaminaComponent"/>. Consumed by the ADS zoom system so
    ///     that aiming-down-sights zoom stacks multiplicatively with exhaustion zoom.
    /// </summary>
    public float CurrentExhaustionZoomScale { get; private set; } = 1.0f;

    private const float StaminaMaxZoom = 0.18f;
    private const float ExhaustionStartRatio = 0.85f;
    private const float ExhaustionMaxZoom = 0.20f;
    private const float ZoomLerpSpeed = 6f;

    private const float HeartbeatMaxJump = 0.1f;
    private const float HeartbeatPeriod = 0.9f;

    public const float CriticalStaminaRatio = 0.15f;

    public override void Initialize()
    {
        base.Initialize();
        UpdatesAfter.Add(typeof(EyeLerpingSystem));
        _heartbeatOverlay = new NivalisStaminaHeartbeatOverlay();
        SubscribeLocalEvent<EyeComponent, GetEyeOffsetEvent>(OnGetEyeOffset);
    }

    public override void FrameUpdate(float frameTime)
    {
        base.FrameUpdate(frameTime);

        var heartbeat = false;

        if (_player.LocalEntity is { } pid && TryComp<NivalisStaminaComponent>(pid, out var stamina))
        {
            var staminaRatio = stamina.Max > 0f ? stamina.Current / stamina.Max : 0f;
            var exhaustionRatio = stamina.ExhaustionMax > 0f ? stamina.Exhaustion / stamina.ExhaustionMax : 0f;

            var zoomScale = 1.0f;
            zoomScale += (1f - staminaRatio) * StaminaMaxZoom;
            if (exhaustionRatio >= ExhaustionStartRatio)
                zoomScale += (exhaustionRatio - ExhaustionStartRatio) / (1f - ExhaustionStartRatio) * ExhaustionMaxZoom;

            var baseZoom = 1.0f;
            if (TryComp<ContentEyeComponent>(pid, out var contentEye))
                baseZoom = Math.Max(SharedContentEyeSystem.MinZoom.X, contentEye.TargetZoom.X);

            var targetZoom = baseZoom / zoomScale;
            CurrentExhaustionZoomScale = zoomScale;
            SetZoom(targetZoom, frameTime);

            heartbeat = stamina.Exhausted || staminaRatio <= CriticalStaminaRatio;
        }
        else
        {
            CurrentExhaustionZoomScale = 1.0f;
            _currentZoom = _eyeManager.CurrentEye.Zoom.X;
        }

        if (heartbeat)
        {
            if (_heartbeatOverlay != null && !_overlay.HasOverlay<NivalisStaminaHeartbeatOverlay>())
                _overlay.AddOverlay(_heartbeatOverlay);
        }
        else if (_heartbeatOverlay != null && _overlay.HasOverlay<NivalisStaminaHeartbeatOverlay>())
        {
            _overlay.RemoveOverlay(_heartbeatOverlay);
        }
    }

    private void SetZoom(float target, float frameTime)
    {
        _currentZoom += (target - _currentZoom) * MathF.Min(1f, frameTime * ZoomLerpSpeed);
        _currentZoom = Math.Clamp(_currentZoom, 0.1f, 2f);
        _eyeManager.CurrentEye.Zoom = new Vector2(_currentZoom, _currentZoom);
    }

    private void OnGetEyeOffset(Entity<EyeComponent> ent, ref GetEyeOffsetEvent args)
    {
        if (ent.Owner != _player.LocalEntity || !TryComp<NivalisStaminaComponent>(ent, out var stamina))
            return;

        var ratio = stamina.Max > 0f ? stamina.Current / stamina.Max : 0f;
        if (!stamina.Exhausted && ratio > CriticalStaminaRatio)
            return;

        var (lub, _) = HeartbeatPulse((float)_timing.RealTime.TotalSeconds);
        args.Offset += new Vector2(0f, -lub * HeartbeatMaxJump);
    }

    internal static (float lub, float dub) HeartbeatPulse(float t)
    {
        const float lubStart = 0.0f;
        const float lubEnd = 0.28f;
        const float dubStart = 0.30f;
        const float dubEnd = 0.48f;

        var beat = t % HeartbeatPeriod;

        var lub = beat >= lubStart && beat < lubEnd
            ? MathF.Sin(((beat - lubStart) / (lubEnd - lubStart)) * MathF.PI)
            : 0f;

        var dub = beat >= dubStart && beat < dubEnd
            ? MathF.Sin(((beat - dubStart) / (dubEnd - dubStart)) * MathF.PI) * 0.55f
            : 0f;

        return (lub, dub);
    }

    public override void Shutdown()
    {
        base.Shutdown();
        if (_heartbeatOverlay != null)
            _overlay.RemoveOverlay(_heartbeatOverlay);
    }
}

