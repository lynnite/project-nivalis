using System.Collections.Generic;
using System.Numerics;
using Content.Shared._Nivalis.GameTicking.Components;
using Content.Shared.Movement.Components;
using Content.Shared.Movement.Systems;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Client.ResourceManagement;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Client._Nivalis.GameTicking;

public sealed partial class NivalisStormSystem : EntitySystem
{
    private static readonly Color CloudColor = new(0.90f, 0.92f, 0.95f, 1f);
    private const float CloudMaxAlpha = 0.20f;

    [Dependency] private IOverlayManager _overlay = default!;
    [Dependency] private IPlayerManager _player = default!;
    [Dependency] private SharedTransformSystem _xforms = default!;
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private IPrototypeManager _protos = default!;
    [Dependency] private IResourceCache _resources = default!;
    [Dependency] private SharedContentEyeSystem _eye = default!;
    [Dependency] private SpriteSystem _sprite = default!;

    private NivalisStormOverlay? _instance;
    private readonly Dictionary<EntityUid, float> _cloudAlpha = new();
    private Vector2 _currentZoomTarget = Vector2.One;

    public override void Initialize()
    {
        base.Initialize();
        _instance = new NivalisStormOverlay(_timing, _random, _protos, _resources);
        _overlay.AddOverlay(_instance);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (_instance == null)
            return;

        if (_player.LocalEntity is { } player
            && TryComp(player, out TransformComponent? playerXform)
            && playerXform != null
            && playerXform.MapID != MapId.Nullspace)
        {
            _instance.PlayerPosition = _xforms.GetWorldPosition(playerXform);
        }

        var inside = IsInsideStorm();
        _instance.TargetIntensity = inside ? 1f : 0f;

        UpdateSmokeClouds(inside, frameTime);
        UpdateZoom(inside, frameTime);
    }

    private void UpdateSmokeClouds(bool inside, float frameTime)
    {
        var dt = MathF.Min(1f, frameTime * 2f);

        var query = EntityQueryEnumerator<NivalisSmokeCloudComponent, TransformComponent, SpriteComponent>();
        while (query.MoveNext(out var uid, out _, out var xform, out var sprite))
        {
            var target = 0f;
            if (TryComp<NivalisStormZoneComponent>(xform.ParentUid, out var zone) && zone.Active && !inside)
                target = 1f;

            _cloudAlpha.TryGetValue(uid, out var current);
            current = Math.Clamp(current + (target - current) * dt, 0f, 1f);
            _cloudAlpha[uid] = current;

            _sprite.SetColor((uid, sprite), new Color(CloudColor.R, CloudColor.G, CloudColor.B, current * CloudMaxAlpha));
        }
    }

    private void UpdateZoom(bool inside, float frameTime)
    {
        if (_player.LocalEntity is not { } player)
            return;

        if (!TryComp(player, out ContentEyeComponent? eye))
            return;

        var desired = inside ? SharedContentEyeSystem.MinZoom : SharedContentEyeSystem.DefaultZoom;
        var rate = inside ? 3f : 8f;
        _currentZoomTarget = Vector2.Lerp(_currentZoomTarget, desired, MathF.Min(1f, frameTime * rate));
        _eye.SetZoom(player, _currentZoomTarget, eye: eye);
    }

    private bool IsInsideStorm()
    {
        var player = _player.LocalEntity;
        if (player == null || !TryComp(player.Value, out TransformComponent? playerXform) || playerXform == null)
            return false;

        if (playerXform.MapID == MapId.Nullspace)
            return false;

        var playerPos = _xforms.GetWorldPosition(playerXform);

        var query = EntityQueryEnumerator<NivalisStormZoneComponent, TransformComponent>();
        while (query.MoveNext(out var _, out var zone, out var xform))
        {
            if (!zone.Active)
                continue;

            if (xform.MapID != playerXform.MapID)
                continue;

            var zonePos = _xforms.GetWorldPosition(xform);
            if ((playerPos - zonePos).Length() <= zone.Radius)
                return true;
        }

        return false;
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
}
