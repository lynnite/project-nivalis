using System.Numerics;
using Content.Shared._Nivalis.Perks;
using Robust.Client.Graphics;
using Robust.Client.Input;
using Robust.Client.Player;
using Robust.Client.ResourceManagement;
using Robust.Shared.Map;

namespace Content.Client._Nivalis.Perks;

public sealed partial class NivalisCrosslinkOverlaySystem : EntitySystem
{
    private const float RecallRange = 35f;

    [Dependency] private IOverlayManager _overlay = default!;
    [Dependency] private IPlayerManager _player = default!;
    [Dependency] private IEyeManager _eye = default!;
    [Dependency] private IInputManager _input = default!;
    [Dependency] private IResourceCache _resources = default!;
    [Dependency] private SharedTransformSystem _transform = default!;

    private NivalisCrosslinkOverlay? _instance;

    public override void Initialize()
    {
        base.Initialize();
        _instance = new NivalisCrosslinkOverlay(_input, _resources);
        _overlay.AddOverlay(_instance);
    }

    public override void Shutdown()
    {
        base.Shutdown();
        if (_instance != null)
        {
            _overlay.RemoveOverlay(_instance);
            _instance = null!;
        }
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (_instance == null)
            return;

        _instance.HasAnyKnife = false;
        _instance.HasInRangeKnife = false;

        if (_player.LocalEntity is not { } local)
            return;

        if (!TryComp<NivalisPerkComponent>(local, out var perk) || perk.Perk?.Id != "Crosslink")
            return;

        var userPos = _transform.GetWorldPosition(local);
        var aim = GetAimDirection();
        var mapId = Transform(local).MapID;

        var query = EntityQueryEnumerator<NivalisCrosslinkKnifeMarkerComponent, TransformComponent>();
        while (query.MoveNext(out _, out _, out var xform))
        {
            if (xform.MapID != mapId)
                continue;

            _instance.HasAnyKnife = true;

            var knifePos = _transform.GetWorldPosition(xform);
            var toKnife = knifePos - userPos;
            var distance = toKnife.Length();
            if (distance < 0.001f || distance > RecallRange)
                continue;

            if (aim.LengthSquared() > 0.0001f)
            {
                var dot = Vector2.Dot(toKnife / distance, aim);
                if (dot < 0.35f)
                    continue;
            }

            _instance.HasInRangeKnife = true;
        }
    }

    private Vector2 GetAimDirection()
    {
        var origin = _eye.CurrentEye.Position.Position;
        var mouse = _eye.ScreenToMap(_input.MouseScreenPosition);
        if (mouse.MapId == MapId.Nullspace)
            return Vector2.Zero;

        var delta = mouse.Position - origin;
        return delta.LengthSquared() < 0.0001f ? Vector2.Zero : Vector2.Normalize(delta);
    }
}
