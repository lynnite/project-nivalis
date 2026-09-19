using System.Numerics;
using Content.Shared._Nivalis.Health;
using Robust.Client.Graphics;
using Robust.Shared.Enums;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Client._Nivalis.Health;

public sealed partial class NivalisBloodOverlay : Overlay
{
    [Dependency] private IEntityManager _ent = default!;
    [Dependency] private IGameTiming _timing = default!;

    public override OverlaySpace Space => OverlaySpace.WorldSpaceBelowEntities;

    private static readonly Vector2 NeckOffset = new(-0.1f, 0f);

    private const float StreakDt = 0.045f;

    private readonly Dictionary<EntityUid, Burst> _bursts = new();

    public NivalisBloodOverlay()
    {
        IoCManager.InjectDependencies(this);
    }

    private sealed class Particle
    {
        public float EmitTime;
        public Vector2 Velocity;
        public float Life;

        public Vector2 Gravity;
        public float Reach;
    }

    private sealed class Burst
    {
        public TimeSpan Start;
        public int Seed;
        public List<Particle> Particles = new();
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        var worldHandle = args.WorldHandle;
        var now = _timing.CurTime;
        var transforms = _ent.System<SharedTransformSystem>();

        var seen = new HashSet<EntityUid>();
        var query = _ent.EntityQueryEnumerator<NivalisBloodSplurtComponent, TransformComponent>();

        while (query.MoveNext(out var uid, out var splurt, out var xform))
        {
            if (xform.MapID != args.MapId)
                continue;

            seen.Add(uid);

            if (!_bursts.TryGetValue(uid, out var burst) || burst.Seed != splurt.Seed)
            {
                burst = new Burst { Start = now, Seed = splurt.Seed };
                GenerateParticles(burst, splurt);
                _bursts[uid] = burst;
            }

            var elapsed = (float)(now - burst.Start).TotalSeconds;
            if (elapsed < 0f || elapsed > splurt.Duration)
                continue;

            DrawBurst(worldHandle, transforms.GetWorldPosition(xform), splurt, burst, elapsed);
        }

        if (_bursts.Count > 0)
        {
            var stale = new List<EntityUid>();
            foreach (var uid in _bursts.Keys)
            {
                if (!seen.Contains(uid))
                    stale.Add(uid);
            }

            foreach (var uid in stale)
                _bursts.Remove(uid);
        }
    }

    private static void GenerateParticles(Burst burst, NivalisBloodSplurtComponent splurt)
    {
        IRobustRandom rng = new RobustRandom();
        rng.SetSeed(burst.Seed);

        var baseDir = splurt.Direction;
        if (baseDir.LengthSquared() < 0.0001f)
            baseDir = new Vector2(-1f, 0f);
        baseDir = Vector2.Normalize(baseDir);

        var spurtDuration = splurt.SpurtDuration;
        var switchover = spurtDuration;
        var transition = MathF.Max(0.001f, splurt.TransitionDuration);

        var spurtLife = splurt.MaxReach / MathF.Max(0.001f, splurt.SpurtSpeed);

        var emitRate = MathF.Max(splurt.SpurtEmitRate, splurt.OozeEmitRate);
        var step = 1f / MathF.Max(1f, emitRate);
        for (var t = 0f; t < splurt.Duration; t += step)
        {
            var after = t >= switchover;

            var blend = SmoothStep(switchover - transition, switchover + transition, t);

            var speed = MathHelper.Lerp(splurt.SpurtSpeed, splurt.OozeSpeed, blend);

            var phaseRate = MathHelper.Lerp(splurt.SpurtEmitRate, splurt.OozeEmitRate, blend);
            if (rng.NextFloat(0f, emitRate) > phaseRate)
                continue;

            var angle = rng.NextFloat(-splurt.SpurtSpread, splurt.SpurtSpread);
            var dir = Rotate(baseDir, angle);

            var jitter = after ? rng.NextFloat(0.7f, 1.3f) : rng.NextFloat(0.85f, 1.15f);
            speed *= jitter;

            burst.Particles.Add(new Particle
            {
                EmitTime = t,
                Velocity = dir * speed,
                Life = after ? rng.NextFloat(0.6f, 1.2f) : spurtLife * rng.NextFloat(0.85f, 1.1f),
                Gravity = after ? splurt.OozeGravity : Vector2.Zero,
                Reach = after ? MathF.Max(0.001f, splurt.OozeReach) : splurt.MaxReach,
            });
        }
    }

    private static float SmoothStep(float edge0, float edge1, float x)
    {
        if (edge1 - edge0 <= 0.0001f)
            return x >= edge1 ? 1f : 0f;

        var t = Math.Clamp((x - edge0) / (edge1 - edge0), 0f, 1f);
        return t * t * (3f - 2f * t);
    }

    private static Vector2 Rotate(Vector2 v, float radians)
    {
        var c = MathF.Cos(radians);
        var s = MathF.Sin(radians);
        return new Vector2(v.X * c - v.Y * s, v.X * s + v.Y * c);
    }

    private void DrawBurst(DrawingHandleWorld handle, Vector2 tileCentre, NivalisBloodSplurtComponent splurt, Burst burst, float elapsed)
    {
        var origin = tileCentre + NeckOffset;
        var color = splurt.Color;

        var overallFade = 1f - Math.Clamp(elapsed / splurt.Duration, 0f, 1f);
        if (overallFade <= 0.01f)
            return;

        foreach (var p in burst.Particles)
        {
            var age = elapsed - p.EmitTime;
            if (age < 0f || age > p.Life)
                continue;

            var lifeFade = 1f - age / p.Life;

            var alpha = overallFade * lifeFade;
            if (alpha <= 0.02f)
                continue;

            var pos = origin + p.Velocity * age + 0.5f * p.Gravity * (age * age);
            var trailAge = MathF.Max(0f, age - StreakDt);
            var prev = origin + p.Velocity * trailAge + 0.5f * p.Gravity * (trailAge * trailAge);

            pos = ClampReach(origin, pos, p.Reach);
            prev = ClampReach(origin, prev, p.Reach);

            if ((pos - prev).LengthSquared() < 0.00004f)
                continue;

            handle.DrawLine(prev, pos, color.WithAlpha(alpha * color.A));
        }
    }

    private static Vector2 ClampReach(Vector2 origin, Vector2 pos, float maxReach)
    {
        var offset = pos - origin;
        if (offset.LengthSquared() > maxReach * maxReach)
            return origin + Vector2.Normalize(offset) * maxReach;

        return pos;
    }
}
