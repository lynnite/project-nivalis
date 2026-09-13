using Content.Shared._Nivalis.Perks;
using Robust.Client.Graphics;

namespace Content.Client._Nivalis.Perks;

public sealed partial class NivalisLazarusBuffOverlaySystem : EntitySystem
{
    [Dependency] private IOverlayManager _overlay = default!;

    private NivalisLazarusBuffOverlay? _instance;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeNetworkEvent<NivalisLazarusBuffMessage>(OnBuff);

        _instance = new NivalisLazarusBuffOverlay();
        _overlay.AddOverlay(_instance);
    }

    private void OnBuff(NivalisLazarusBuffMessage msg)
    {
        _instance?.Begin(msg.Duration);
    }

    public override void FrameUpdate(float frameTime)
    {
        base.FrameUpdate(frameTime);

        if (_instance is { Remaining: > 0f })
            _instance.Remaining = MathF.Max(0f, _instance.Remaining - frameTime);
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
