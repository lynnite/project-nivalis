using System.Numerics;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Analyzers;
using Robust.Shared.Maths;

namespace Content.Client._Nivalis.UserInterface.Lobby;

[Virtual]
public class SlantedButton : Button
{
    public float Shear
    {
        get => _shear;
        set
        {
            if (MathHelper.CloseTo(_shear, value))
                return;

            _shear = value;
        }
    }

    public Vector2 VisualScale
    {
        get => _visualScale;
        set
        {
            if (MathHelper.CloseTo(_visualScale.X, value.X) && MathHelper.CloseTo(_visualScale.Y, value.Y))
                return;

            _visualScale = value;
        }
    }

    private float _shear;
    private Vector2 _visualScale = Vector2.One;

    public Vector2 VisualOffset
    {
        get => _visualOffset;
        set
        {
            if (MathHelper.CloseTo(_visualOffset.X, value.X) && MathHelper.CloseTo(_visualOffset.Y, value.Y))
                return;

            _visualOffset = value;
        }
    }

    private Vector2 _visualOffset = Vector2.Zero;

    protected override void Draw(DrawingHandleScreen handle)
    {
        var oldXform = handle.GetTransform();
        handle.SetTransform(oldXform * GetContentTransform());
        base.Draw(handle);
        handle.SetTransform(oldXform);
    }

    protected override void RenderChildOverride(ref ControlRenderArguments args, int childIndex, Vector2i position)
    {
        var handle = args.Handle.DrawingHandleScreen;
        var oldXform = handle.GetTransform();

        var pos = (Vector2)position;
        handle.SetTransform(oldXform
            * Matrix3x2.CreateTranslation(pos)
            * GetContentTransform()
            * Matrix3x2.CreateTranslation(-pos));

        base.RenderChildOverride(ref args, childIndex, position);
        handle.SetTransform(oldXform);
    }

    private Matrix3x2 GetContentTransform()
    {
        if (_shear == 0f
            && MathHelper.CloseTo(_visualScale.X, 1f)
            && MathHelper.CloseTo(_visualScale.Y, 1f)
            && _visualOffset == Vector2.Zero)
        {
            return Matrix3x2.Identity;
        }

        var size = PixelSize;

        var slope = size.X > 0 ? _shear / size.X : 0f;
        var shearMatrix = new Matrix3x2(1f, slope, 0f, 1f, 0f, 0f);

        var pivot = size / 2f;
        var scale = Matrix3x2.CreateScale(_visualScale);
        var offset = Matrix3x2.CreateTranslation(_visualOffset);

        return Matrix3x2.CreateTranslation(-pivot)
               * scale
               * offset
               * shearMatrix
               * Matrix3x2.CreateTranslation(pivot);
    }
}
