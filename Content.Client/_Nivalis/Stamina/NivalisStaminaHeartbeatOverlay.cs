using Content.Shared._Nivalis.Stamina;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Shared.Enums;
using Robust.Shared.Maths;
using Robust.Shared.Timing;

namespace Content.Client._Nivalis.Stamina;

public sealed partial class NivalisStaminaHeartbeatOverlay : Overlay
{
    [Dependency] private IEntityManager _ent = default!;
    [Dependency] private IPlayerManager _player = default!;
    [Dependency] private IGameTiming _timing = default!;

    public override OverlaySpace Space => OverlaySpace.ScreenSpace;
    public override bool RequestScreenTexture => true;

    private const float Coldown = 0.7f;
    private const float MaxAlpha = 0.85f;

    private float _dizzinessRemaining;
    private bool _wasBeating;

    public NivalisStaminaHeartbeatOverlay()
    {
        IoCManager.InjectDependencies(this);
    }

    protected override void FrameUpdate(FrameEventArgs args)
    {
        base.FrameUpdate(args);

        _dizzinessRemaining = MathF.Max(0f, _dizzinessRemaining - args.DeltaSeconds);

        var (lub, _) = NivalisStaminaEffectsSystem.HeartbeatPulse((float)_timing.RealTime.TotalSeconds);
        var beating = lub > 0.5f;
        if (beating && !_wasBeating)
            _dizzinessRemaining = Coldown;

        _wasBeating = beating;
    }

    protected override bool BeforeDraw(in OverlayDrawArgs args)
    {
        var entity = _player.LocalEntity;
        if (entity == null || !_ent.TryGetComponent(entity.Value, out NivalisStaminaComponent? comp))
            return false;

        var ratio = comp.Max > 0f ? comp.Current / comp.Max : 0f;
        return comp.Exhausted || ratio <= NivalisStaminaEffectsSystem.CriticalStaminaRatio;
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        if (ScreenTexture == null || _dizzinessRemaining <= 0f)
            return;

        var alpha = (_dizzinessRemaining / Coldown) * MaxAlpha;
        var color = new Color(1f, 1f, 1f, alpha);
        var bounds = args.ViewportBounds;
        var rect = new UIBox2(bounds.Left, bounds.Top, bounds.Right, bounds.Bottom);
        args.ScreenHandle.DrawTextureRect(ScreenTexture, rect, color);
    }
}
