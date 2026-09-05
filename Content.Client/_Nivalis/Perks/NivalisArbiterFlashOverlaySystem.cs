using Content.Shared._Nivalis.Perks;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Shared.Maths;

namespace Content.Client._Nivalis.Perks;

/// <summary>
///     Receives the Arbiter flash cues (yellow ready prompt and white longshot discharge) and
///     drives a brief full-screen opaque colour flash for the local Arbiter player.
/// </summary>
public sealed partial class NivalisArbiterFlashOverlaySystem : EntitySystem
{
    [Dependency] private IOverlayManager _overlay = default!;
    [Dependency] private IPlayerManager _player = default!;

    private NivalisArbiterFlashOverlay? _instance;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeNetworkEvent<NivalisArbiterFlashMessage>(OnArbiterFlash);

        _instance = new NivalisArbiterFlashOverlay();
        _overlay.AddOverlay(_instance);
    }

    private void OnArbiterFlash(NivalisArbiterFlashMessage msg)
    {
        if (_instance == null)
            return;

        // Only the player who actually fired/cued should see the flash.
        if (_player.LocalEntity is not { } local)
            return;

        if (GetEntity(msg.Source) != local)
            return;

        var color = new Color(msg.Red, msg.Green, msg.Blue);
        _instance.Begin(color, msg.HoldTime, msg.FadeTime);
    }

    public override void FrameUpdate(float frameTime)
    {
        base.FrameUpdate(frameTime);
        _instance?.Tick(frameTime);
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
