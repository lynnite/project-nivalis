using Content.Shared._Nivalis.Status;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Shared.Enums;

namespace Content.Client._Nivalis.Status;

public sealed partial class NivalisBleedOverlay : Overlay
{
    [Dependency] private IEntityManager _ent = default!;
    [Dependency] private IPlayerManager _player = default!;

    public override OverlaySpace Space => OverlaySpace.ScreenSpace;

    private const float DimStartPercent = 5f;
    private const float DimEndPercent = 10f;

    private const float MaxDimAlpha = 0.15f;

    public NivalisBleedOverlay()
    {
        IoCManager.InjectDependencies(this);
    }

    protected override bool BeforeDraw(in OverlayDrawArgs args)
    {
        var entity = _player.LocalEntity;
        if (entity == null)
            return false;

        if (!_ent.TryGetComponent(entity.Value, out NivalisBleedDecalComponent? comp))
            return false;

        return GetDimAlpha(comp.SpawnCount) > 0f;
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        var entity = _player.LocalEntity;
        if (entity == null || !_ent.TryGetComponent(entity.Value, out NivalisBleedDecalComponent? comp))
            return;

        var alpha = GetDimAlpha(comp.SpawnCount);
        if (alpha <= 0f)
            return;

        var alphaColor = new Color(0f, 0f, 0f, alpha);
        args.ScreenHandle.DrawRect(args.ViewportBounds, alphaColor);
    }

    private static float GetDimAlpha(int spawnCount)
    {
        var slowPercent = Math.Min(spawnCount * 0.5f, DimEndPercent);
        if (slowPercent < DimStartPercent)
            return 0f;

        var t = (slowPercent - DimStartPercent) / (DimEndPercent - DimStartPercent);
        return Math.Clamp(t * MaxDimAlpha, 0f, MaxDimAlpha);
    }
}


