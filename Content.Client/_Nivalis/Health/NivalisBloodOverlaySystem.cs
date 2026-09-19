using Robust.Client.Graphics;
using Robust.Shared.GameObjects;

namespace Content.Client._Nivalis.Health;

public sealed partial class NivalisBloodOverlaySystem : EntitySystem
{
    [Dependency] private IOverlayManager _overlay = default!;

    private NivalisBloodOverlay? _overlayInstance;

    public override void Initialize()
    {
        base.Initialize();

        _overlayInstance = new NivalisBloodOverlay();
        _overlay.AddOverlay(_overlayInstance);
    }

    public override void Shutdown()
    {
        base.Shutdown();

        if (_overlayInstance != null)
        {
            _overlay.RemoveOverlay(_overlayInstance);
            _overlayInstance = null;
        }
    }
}
