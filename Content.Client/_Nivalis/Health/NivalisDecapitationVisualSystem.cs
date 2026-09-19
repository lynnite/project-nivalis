using System.Numerics;
using Content.Shared._Nivalis.Health;
using Content.Shared.Humanoid;
using Robust.Client.GameObjects;
using Robust.Shared.GameObjects;

namespace Content.Client._Nivalis.Health;

public sealed partial class NivalisDecapitationVisualSystem : EntitySystem
{
    [Dependency] private SpriteSystem _sprite = default!;
    [Dependency] private AppearanceSystem _appearance = default!;

    private static readonly HumanoidVisualLayers[] HeadLayers =
    {
        HumanoidVisualLayers.Head,
        HumanoidVisualLayers.HeadSide,
        HumanoidVisualLayers.HeadTop,
        HumanoidVisualLayers.Snout,
        HumanoidVisualLayers.SnoutCover,
        HumanoidVisualLayers.Eyes,
        HumanoidVisualLayers.Hair,
        HumanoidVisualLayers.FacialHair,
    };

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<NivalisDecapitatedComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<NivalisDecapitatedComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<NivalisDecapitatedComponent, AfterAutoHandleStateEvent>(OnHandleState);
        SubscribeLocalEvent<NivalisDecapitatedComponent, HumanoidLayerVisibilityChangedEvent>(OnLayerVisibilityChanged);
    }

    private void OnStartup(EntityUid uid, NivalisDecapitatedComponent comp, ComponentStartup args)
    {
        ApplyDecapitation(uid, comp, true);
    }

    private void OnShutdown(EntityUid uid, NivalisDecapitatedComponent comp, ComponentShutdown args)
    {
        ApplyDecapitation(uid, comp, false);
    }

    private void OnHandleState(EntityUid uid, NivalisDecapitatedComponent comp, ref AfterAutoHandleStateEvent args)
    {
        ApplyDecapitation(uid, comp, true);
    }

    private void OnLayerVisibilityChanged(EntityUid uid, NivalisDecapitatedComponent comp, ref HumanoidLayerVisibilityChangedEvent args)
    {
        if (Array.IndexOf(HeadLayers, args.Layer) >= 0)
            args.ShouldHide = true;
    }

    private void ApplyDecapitation(EntityUid uid, NivalisDecapitatedComponent comp, bool decapitated)
    {
        if (!TryComp<SpriteComponent>(uid, out var sprite))
            return;

        foreach (var layer in HeadLayers)
        {
            if (_sprite.LayerMapTryGet((uid, sprite), layer, out var index, false))
                _sprite.LayerSetVisible((uid, sprite), index, !decapitated);
        }

        if (TryComp<AppearanceComponent>(uid, out var appearance))
            _appearance.SetData(uid, HumanoidVisualLayers.Head, decapitated, appearance);

        if (_sprite.LayerMapTryGet((uid, sprite), "head", out var headGear, false))
            _sprite.LayerSetVisible((uid, sprite), headGear, !decapitated);

        var offset = decapitated ? new Vector2(0f, comp.SpriteOffset) : Vector2.Zero;
        _sprite.SetOffset((uid, sprite), offset);
    }
}

