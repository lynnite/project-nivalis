using System;
using Robust.Client.Graphics;
using Robust.Shared.Enums;
using Robust.Shared.Maths;

namespace Content.Client._Nivalis.Perks;

public sealed partial class NivalisExecutionerCrtOverlay : Overlay
{
    public override OverlaySpace Space => OverlaySpace.ScreenSpace;

    private float _blot;

    private bool _worn;

    public NivalisExecutionerCrtOverlay()
    {
    }

    public void SetState(float blot, bool worn = false)
    {
        _blot = Math.Clamp(blot, 0f, 1f);
        _worn = worn;
    }

    protected override bool BeforeDraw(in OverlayDrawArgs args)
    {
        return _blot > 0.01f || _worn;
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        if (!BeforeDraw(args))
            return;

        var bounds = args.ViewportBounds;
        var blot = _blot;

        if (_worn)
        {
            var redAlpha = 0.30f * Math.Clamp(blot, 0f, 1f) + 0.12f;
            args.ScreenHandle.DrawRect(bounds, new Color(0.45f, 0.02f, 0.02f).WithAlpha(redAlpha));

            if (blot < 0.95f)
            {
                const float stripeH = 3f;
                for (var y = (float)bounds.Bottom; y < bounds.Top; y += stripeH * 2f)
                {
                    var line = new UIBox2(bounds.Left, y, bounds.Right, Math.Min(y + stripeH, bounds.Top));
                    args.ScreenHandle.DrawRect(line, new Color(0f, 0f, 0f).WithAlpha(0.06f + blot * 0.10f));
                }
            }
        }

        if (blot > 0.01f)
            args.ScreenHandle.DrawRect(bounds, new Color(0f, 0f, 0f).WithAlpha(blot));
    }
}
