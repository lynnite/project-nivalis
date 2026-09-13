using System.Numerics;
using Robust.Client.Graphics;
using Robust.Shared.Enums;
using Robust.Shared.Prototypes;

namespace Content.Client._Nivalis.Perks;

public sealed partial class NivalisLazarusBuffOverlay : Overlay
{
    private static readonly ProtoId<ShaderPrototype> CircleMaskShader = "GradientCircleMask";

    [Dependency] private IPrototypeManager _prototypeManager = default!;

    public override OverlaySpace Space => OverlaySpace.WorldSpace;

    private readonly ShaderInstance _shader;

    public float Remaining;

    public float Duration = 1f;

    private const float PeakAlpha = 0.5f;

    private const float FadeIn = 0.35f;
    private const float FadeOut = 1.0f;

    public NivalisLazarusBuffOverlay()
    {
        IoCManager.InjectDependencies(this);
        _shader = _prototypeManager.Index(CircleMaskShader).InstanceUnique();
    }

    public void Begin(float duration)
    {
        Duration = MathF.Max(0.1f, duration);
        Remaining = Duration;
    }

    protected override bool BeforeDraw(in OverlayDrawArgs args)
    {
        return Remaining > 0f;
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        if (Remaining <= 0f)
            return;

        var viewport = args.WorldAABB;
        var handle = args.WorldHandle;
        var distance = args.ViewportBounds.Width;

        var elapsed = Duration - Remaining;
        var alpha = PeakAlpha;
        if (elapsed < FadeIn)
            alpha *= elapsed / FadeIn;
        if (Remaining < FadeOut)
            alpha *= Remaining / FadeOut;

        var outerRadius = 1.6f * distance;
        var innerRadius = 0.75f * distance;

        _shader.SetParameter("time", 0f);
        _shader.SetParameter("color", new Vector3(0.25f, 0.7f, 1f));
        _shader.SetParameter("darknessAlphaOuter", alpha);
        _shader.SetParameter("outerCircleRadius", outerRadius);
        _shader.SetParameter("outerCircleMaxRadius", outerRadius + 0.2f * distance);
        _shader.SetParameter("innerCircleRadius", innerRadius);
        _shader.SetParameter("innerCircleMaxRadius", innerRadius + 0.3f * distance);
        handle.UseShader(_shader);
        handle.DrawRect(viewport, Color.White);
        handle.UseShader(null);
    }
}
