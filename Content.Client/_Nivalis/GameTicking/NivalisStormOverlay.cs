using System;
using System.Collections.Generic;
using System.Numerics;
using Content.Client.Resources;
using Robust.Client.Graphics;
using Robust.Client.ResourceManagement;
using Robust.Shared.Enums;
using Robust.Shared.Maths;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Client._Nivalis.GameTicking;

public sealed partial class NivalisStormOverlay : Overlay
{
    private static readonly ProtoId<ShaderPrototype> MaskShaderId = "NivalisStormMask";
    private static readonly ProtoId<ShaderPrototype> DistortShaderId = "NivalisStormDistort";

    private const float ClearRadiusTiles = 1.5f;
    private const int MaxSnowflakes = 230;

    private readonly IGameTiming _timing;
    private readonly IRobustRandom _random;
    private readonly IPrototypeManager _protos;
    private readonly IResourceCache _resources;

    private ShaderInstance? _maskShader;
    private ShaderInstance? _distortShader;
    private Texture? _noiseTexture;
    private bool _shadersReady;

    private readonly List<Snowflake> _snowflakes = new();

    public float TargetIntensity;

    public Vector2 PlayerPosition;

    private float _intensity;
    private float _accumulator;

    public override OverlaySpace Space => OverlaySpace.ScreenSpace;
    public override bool RequestScreenTexture => true;

    private sealed class Snowflake
    {
        public Vector2 Position;
        public Vector2 Velocity;
        public float Size;
        public float Alpha;
    }

    public NivalisStormOverlay(IGameTiming timing, IRobustRandom random, IPrototypeManager protos, IResourceCache resources)
    {
        _timing = timing;
        _random = random;
        _protos = protos;
        _resources = resources;
    }

    private void EnsureShaders()
    {
        if (_shadersReady)
            return;

        try
        {
            _maskShader = _protos.Index(MaskShaderId).InstanceUnique();
            _distortShader = _protos.Index(DistortShaderId).InstanceUnique();
            _noiseTexture = _resources.GetTexture("/Textures/Effects/HeatBlur/perlin_noise.png");
        }
        catch
        {
        }
        finally
        {
            _shadersReady = true;
        }
    }

    protected override bool BeforeDraw(in OverlayDrawArgs args)
    {
        return _intensity > 0.005f || TargetIntensity > 0.005f;
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        var viewport = args.ViewportBounds;
        var handle = args.ScreenHandle;

        var frameTime = (float)_timing.FrameTime.TotalSeconds;

        var diff = TargetIntensity - _intensity;
        var animRate = diff > 0f ? 10f : 14f;
        _intensity = Math.Clamp(_intensity + diff * MathF.Min(1f, frameTime * animRate), 0f, 1f);

        if (_intensity <= 0.005f)
            return;

        EnsureShaders();

        var w = viewport.Width;
        var h = viewport.Height;
        if (w <= 0 || h <= 0)
            return;

        if (ScreenTexture != null && _distortShader != null && _noiseTexture != null)
        {
            _distortShader.SetParameter("SCREEN_TEXTURE", ScreenTexture);
            _distortShader.SetParameter("NOISE_TEXTURE", _noiseTexture);
            _distortShader.SetParameter("grainScale", 640f);
            _distortShader.SetParameter("intensity", _intensity);
            handle.UseShader(_distortShader);
            handle.DrawRect(viewport, Color.White);
            handle.UseShader(null);
        }

        _accumulator += frameTime;
        while (_accumulator > 0.02f && _snowflakes.Count < MaxSnowflakes)
        {
            _accumulator -= 0.02f;
            SpawnSnowflake(viewport);
        }

        foreach (var flake in _snowflakes)
        {
            flake.Position += flake.Velocity * frameTime;
        }

        handle.DrawRect(viewport, new Color(0.86f, 0.88f, 0.93f, 0.14f * _intensity));

        foreach (var flake in _snowflakes)
        {
            if (flake.Position.X < viewport.Left || flake.Position.Y > viewport.Bottom)
                continue;

            var flakeColor = new Color(0.88f + 0.12f * flake.Alpha, 0.92f + 0.08f * flake.Alpha, 0.98f + 0.02f * flake.Alpha, (0.85f + 0.15f * flake.Alpha) * _intensity);

            var rect = new UIBox2(
                flake.Position.X - flake.Size * 0.5f,
                flake.Position.Y - flake.Size * 0.5f,
                flake.Position.X + flake.Size * 0.5f,
                flake.Position.Y + flake.Size * 0.5f);

            handle.DrawRect(rect, flakeColor);
        }

        _snowflakes.RemoveAll(f => f.Position.X < viewport.Left || f.Position.Y > viewport.Bottom);

        if (_maskShader == null)
            return;

        var world = args.WorldAABB;
        if (world.Width <= 0.01f || world.Height <= 0.01f)
            return;

        var pxPerMeter = viewport.Width / world.Width;

        var innerRadius = ClearRadiusTiles * pxPerMeter;
        var outerRadius = innerRadius + 3.5f * pxPerMeter;

        var px = (PlayerPosition.X - world.Left) / world.Width * viewport.Width;
        var t = (PlayerPosition.Y - world.Bottom) / world.Height;
        var py = viewport.Height * (1f - t);
        var center = new Vector2(px, viewport.Height - py);

        _maskShader.SetParameter("viewportSize", new Vector2(viewport.Width, viewport.Height));
        _maskShader.SetParameter("center", center);
        _maskShader.SetParameter("whiteColor", new Vector3(0.90f, 0.92f, 0.96f));
        _maskShader.SetParameter("maxAlpha", 1f * _intensity);
        _maskShader.SetParameter("innerRadius", innerRadius);
        _maskShader.SetParameter("outerRadius", outerRadius);

        handle.UseShader(_maskShader);
        handle.DrawRect(viewport, Color.White);
        handle.UseShader(null);
    }

    private void SpawnSnowflake(UIBox2 viewport)
    {
        var flake = new Snowflake
        {
            Position = new Vector2(
                _random.NextFloat(viewport.Right * 0.2f, viewport.Right),
                _random.NextFloat(viewport.Top, viewport.Top + (viewport.Height * 0.5f))),
            Velocity = new Vector2(
                -_random.NextFloat(260f, 440f),
                _random.NextFloat(300f, 480f)),
            Size = _random.NextFloat(1f, 2f),
            Alpha = _random.NextFloat(0.0f, 1f),
        };

        _snowflakes.Add(flake);
    }
}
