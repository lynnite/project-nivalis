using Robust.Client.Graphics;
using Robust.Shared.Maths;

namespace Content.Client._Nivalis.UserInterface.Lobby;

public sealed class ScrollingScanlineStyleBox : StyleBox
{
    public Texture? Texture { get; set; }

    public Color BackgroundColor { get; set; } = new(0.58f, 0.82f, 1.0f);

    public Color Modulate { get; set; } = Color.White;

    public float ScrollOffset { get; set; }

    protected override void DoDraw(DrawingHandleScreen handle, UIBox2 box, float uiScale)
    {
        handle.DrawRect(box, BackgroundColor * Modulate);

        if (Texture == null || Texture.Height == 0)
            return;

        var tileH = Texture.Height * uiScale;

        var scrolled = (ScrollOffset * uiScale) % tileH;
        if (scrolled < 0)
            scrolled += tileH;

        for (var y = box.Top - scrolled; y < box.Bottom; y += tileH)
        {
            var destTop = System.MathF.Max(y, box.Top);
            var destBottom = System.MathF.Min(y + tileH, box.Bottom);

            if (destBottom <= destTop)
                continue;

            var srcTop = destTop - y;

            var dest = new UIBox2(box.Left, destTop, box.Right, destBottom);
            var src = new UIBox2(0, srcTop, Texture.Width, srcTop + (destBottom - destTop));

            handle.DrawTextureRectRegion(Texture, dest, src, Modulate);
        }
    }
}
