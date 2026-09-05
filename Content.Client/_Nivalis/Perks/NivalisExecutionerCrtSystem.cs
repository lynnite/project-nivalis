using Content.Shared._Nivalis.Perks;
using Robust.Client.Graphics;
using Robust.Client.Player;

namespace Content.Client._Nivalis.Perks;

public sealed partial class NivalisExecutionerCrtSystem : EntitySystem
{
    [Dependency] private IOverlayManager _overlay = default!;
    [Dependency] private IPlayerManager _player = default!;

    private const float BaseOpacity = 0.12f;

    private const float CriticalThreshold = 0.10f;

    private const float BlackHoldFrac = 0.12f;

    private const float BlinkDuration = 1f;

    private NivalisExecutionerCrtOverlay? _instance;

    private float _blink = -1f;

    private bool _prevLive;

    public override void Initialize()
    {
        base.Initialize();

        _instance = new NivalisExecutionerCrtOverlay();
        _overlay.AddOverlay(_instance);
    }

    private static float SteadyOpacity(bool live, bool broken, float durabilityPct)
    {
        if (!live)
            return 0f;

        if (broken)
            return 1f;

        if (durabilityPct >= CriticalThreshold)
            return BaseOpacity;

        var t = Math.Clamp((CriticalThreshold - durabilityPct) / CriticalThreshold, 0f, 1f);
        return MathHelper.Lerp(BaseOpacity, 1f, t);
    }

    public override void FrameUpdate(float frameTime)
    {
        base.FrameUpdate(frameTime);

        if (_instance == null)
            return;

        var live = false;
        var broken = false;
        var durabilityPct = 1f;

        if (_player.LocalEntity is { } local &&
            TryComp<NivalisExecutionerComponent>(local, out var exec))
        {
            live = exec.Live;
            broken = exec.Broken;
            durabilityPct = exec.MaxDurability > 0f
                ? Math.Clamp(exec.Durability / exec.MaxDurability, 0f, 1f)
                : 0f;
        }

        if (live != _prevLive)
        {
            _prevLive = live;
            _blink = 0f;
        }

        var p = 0f;
        var active = _blink >= 0f;
        if (active)
        {
            if (_blink >= BlinkDuration)
            {
                _blink = -1f;
                active = false;
                p = 1f;
            }
            else
            {
                _blink += frameTime;
                p = Math.Clamp(_blink / BlinkDuration, 0f, 1f);
            }
        }

        var steady = SteadyOpacity(live, broken, durabilityPct);

        float blot;
        if (!active)
        {
            blot = steady;
        }
        else
        {
            if (p <= BlackHoldFrac)
            {
                blot = 1f;
            }
            else
            {
                var q = Math.Clamp((p - BlackHoldFrac) / (1f - BlackHoldFrac), 0f, 1f);
                blot = MathHelper.Lerp(1f, steady, q);
            }
        }

        _instance.SetState(blot, worn: live);
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

