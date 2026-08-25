using Content.Shared._Nivalis.Status;
using Robust.Client.Graphics;
using Robust.Client.Player;

namespace Content.Client._Nivalis.Status;

public sealed partial class NivalisBleedOverlaySystem : EntitySystem
{
    [Dependency] private IOverlayManager _overlay = default!;
    [Dependency] private IPlayerManager _player = default!;

    private NivalisBleedOverlay? _instance;

    public override void Initialize()
    {
        base.Initialize();
        _instance = new NivalisBleedOverlay();
    }

    public override void FrameUpdate(float frameTime)
    {
        base.FrameUpdate(frameTime);

        if (_instance == null)
            return;

        var active = _player.LocalEntity != null &&
                     CompOrNull<NivalisBleedDecalComponent>(_player.LocalEntity) != null;

        if (active && !_overlay.HasOverlay<NivalisBleedOverlay>())
            _overlay.AddOverlay(_instance);
        else if (!active && _overlay.HasOverlay<NivalisBleedOverlay>())
            _overlay.RemoveOverlay(_instance);
    }

    public override void Shutdown()
    {
        base.Shutdown();
        if (_instance != null)
        {
            _overlay.RemoveOverlay(_instance);
            _instance = null;
        }
    }
}

