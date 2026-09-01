using System;
using System.Numerics;
using Robust.Client.Graphics;
using Robust.Shared.Maths;

namespace Content.Client._Nivalis.UserInterface.Lobby;

public sealed class ScrollingScanlineStyleBox : StyleBox
{
    public Texture? Texture { get; set; }

    public Color BackgroundColor { get; set; } = new(0.58f, 0.82f, 1.0f);

    public Color Modulate { get; set; } = Color.White;

    public Color BorderColor { get; set; } = Color.Transparent;

    public float BorderThickness { get; set; } = 1f;

    public float InwardLean { get; set; }

    public float FadeRight { get; set; }

    public float ScrollOffset { get; set; }

    protected override void DoDraw(DrawingHandleScreen handle, UIBox2 box, float uiScale)
    {
        var midY = (box.Top + box.Bottom) / 2f;
        var halfH = box.Height / 2f;
        var lean = Math.Clamp(InwardLean, -0.4f, 0.4f);

        var lTop = midY - halfH * (1f - lean);
        var lBot = midY + halfH * (1f - lean);
        var rTop = midY - halfH * (1f + lean);
        var rBot = midY + halfH * (1f + lean);

        lTop = Math.Clamp(lTop, box.Top, box.Bottom);
        lBot = Math.Clamp(lBot, box.Top, box.Bottom);
        rTop = Math.Clamp(rTop, box.Top, box.Bottom);
        rBot = Math.Clamp(rBot, box.Top, box.Bottom);

        var fade = Math.Clamp(FadeRight, 0f, 1f);
        var baseColor = BackgroundColor * Modulate;
        var leftColor = baseColor;
        var rightColor = Color.InterpolateBetween(baseColor, Color.Transparent, fade);

        var segments = 24;
        for (var s = 0; s < segments; s++)
        {
            var s0 = s / (float) segments;
            var s1 = (s + 1) / (float) segments;
            var mid = (s0 + s1) / 2f;

            var xa = box.Left + (box.Right - box.Left) * s0;
            var xb = box.Left + (box.Right - box.Left) * s1;

            var yTopA = lTop + (rTop - lTop) * s0;
            var yBotA = lBot + (rBot - lBot) * s0;
            var yTopB = lTop + (rTop - lTop) * s1;
            var yBotB = lBot + (rBot - lBot) * s1;

            var col = Color.InterpolateBetween(leftColor, rightColor, mid);
            Span<Vector2> quad = new Vector2[]
            {
                new(xa, yBotA),
                new(xb, yBotB),
                new(xa, yTopA),
                new(xb, yTopB),
            };
            handle.DrawPrimitives(DrawPrimitiveTopology.TriangleStrip, quad, col);
        }

        if (Texture != null && Texture.Height > 0)
        {
            var tileH = Texture.Height * uiScale;
            var top = Math.Max(lTop, rTop);
            var bottom = Math.Min(lBot, rBot);

            if (bottom > top)
            {
                var scrolled = (ScrollOffset * uiScale) % tileH;
                if (scrolled < 0)
                    scrolled += tileH;

                for (var y = top - scrolled + tileH; y < bottom; y += tileH)
                {
                    var destTop = System.MathF.Max(y, top);
                    var destBottom = System.MathF.Min(y + tileH, bottom);
                    if (destBottom <= destTop)
                        continue;

                    var srcTop = destTop - y;
                    var dest = new UIBox2(box.Left, destTop, box.Right, destBottom);
                    var src = new UIBox2(0, srcTop, Texture.Width, srcTop + (destBottom - destTop));

                    var strip = Color.White * Modulate;
                    if (fade > 0f)
                        strip = Color.InterpolateBetween(strip, Color.Transparent, fade * 0.5f);
                    handle.DrawTextureRectRegion(Texture, dest, src, strip);
                }
            }
        }

        if (BorderColor.A > 0f && BorderThickness > 0f)
        {
            handle.DrawLine(new Vector2(box.Left, lTop), new Vector2(box.Right, rTop), BorderColor);
            handle.DrawLine(new Vector2(box.Right, rTop), new Vector2(box.Right, rBot), BorderColor);
            handle.DrawLine(new Vector2(box.Left, lBot), new Vector2(box.Right, rBot), BorderColor);
            handle.DrawLine(new Vector2(box.Left, lTop), new Vector2(box.Left, lBot), BorderColor);
        }
    }
}
