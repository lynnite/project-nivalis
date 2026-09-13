using System.Numerics;
using Content.Client.Resources;
using Robust.Client.Graphics;
using Robust.Client.Input;
using Robust.Client.ResourceManagement;
using Robust.Shared.Enums;
using Robust.Shared.Map;
using Robust.Shared.Maths;

namespace Content.Client._Nivalis.Perks;

public sealed partial class NivalisCrosslinkOverlay : Overlay
{
    private static readonly Color InRangeColor = new(0.95f, 0.15f, 0.15f, 0.9f);
    private static readonly Color OutOfRangeColor = new(0.95f, 0.95f, 0.95f, 0.75f);

    private readonly IInputManager _input;
    private readonly IResourceCache _resources;

    public override OverlaySpace Space => OverlaySpace.ScreenSpace;

    public bool HasInRangeKnife;

    public bool HasAnyKnife;

    private Texture? _icon;

    public NivalisCrosslinkOverlay(IInputManager input, IResourceCache resources)
    {
        _input = input;
        _resources = resources;
    }

    protected override bool BeforeDraw(in OverlayDrawArgs args)
    {
        return HasAnyKnife;
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        if (_icon == null)
        {
            try
            {
                _icon = _resources.GetTexture("/Textures/_Nivalis/Effects/crosslink.rsi/pole.png");
            }
            catch
            {
                return;
            }
        }

        var mouse = _input.MouseScreenPosition;
        if (mouse.Window == WindowId.Invalid)
            return;

        var handle = args.ScreenHandle;
        var color = HasInRangeKnife ? InRangeColor : OutOfRangeColor;

        var pos = mouse.Position + new Vector2(18f, -18f);
        handle.DrawTexture(_icon, pos, color);
    }
}
