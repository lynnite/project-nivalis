using Robust.Client.Graphics;
using Robust.Shared.Enums;
using Robust.Shared.Maths;

namespace Content.Client._Nivalis.Perks;

/// <summary>
///     A full-screen, translucent colour flash used as a timing cue (yellow "press again" just
///     before a readied shortshot auto-fires) and as the white discharge flash of a longshot.
///     Stayed translucent/see-through rather than a solid opaque cover. Driven by
///     <see cref="NivalisArbiterFlashOverlaySystem"/>.
/// </summary>
public sealed partial class NivalisArbiterFlashOverlay : Overlay
{
    public override OverlaySpace Space => OverlaySpace.ScreenSpace;

    private Color _color = Color.White;
    private float _hold;
    private float _fade;
    private float _age;
    private bool _active;

    /// <summary>Peak opacity of the flash. Transparent/see-through so it never whites-out the screen.</summary>
    private const float PeakAlpha = 0.3f;

    public NivalisArbiterFlashOverlay()
    {
    }

    /// <summary>Begins a new full-screen flash.</summary>
    public void Begin(Color color, float hold, float fade)
    {
        _color = color;
        _hold = hold;
        _fade = fade;
        _age = 0f;
        _active = true;
    }

    /// <summary>Advances the flash each frame. Returns true while it is still drawing.</summary>
    public bool Tick(float frameTime)
    {
        if (!_active)
            return false;

        _age += frameTime;
        if (_age >= _hold + _fade)
        {
            _active = false;
            return false;
        }

        return true;
    }

    protected override bool BeforeDraw(in OverlayDrawArgs args)
    {
        return _active;
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        if (!_active)
            return;

        // Rise quickly to a translucent peak, hold through _hold, then transparently fade out.
        float alpha;
        if (_age < 0.05f)
            alpha = PeakAlpha * (_age / 0.05f);          // fast fade-in
        else if (_age <= _hold)
            alpha = PeakAlpha;                            // translucent hold
        else
            alpha = PeakAlpha * MathF.Max(0f, 1f - (_age - _hold) / MathF.Max(_fade, 0.0001f));

        var draw = _color.WithAlpha(alpha);
        args.ScreenHandle.DrawRect(args.ViewportBounds, draw);
    }
}
