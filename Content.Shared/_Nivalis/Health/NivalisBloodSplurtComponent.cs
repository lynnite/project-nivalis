using System.Numerics;
using Robust.Shared.GameStates;
using Robust.Shared.Maths;

namespace Content.Shared._Nivalis.Health;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class NivalisBloodSplurtComponent : Component
{
    [DataField, AutoNetworkedField]
    public int Seed;

    [DataField, AutoNetworkedField]
    public Vector2 Direction = new(-1f, 0f);

    [DataField, AutoNetworkedField]
    public float SpurtDuration = 0.6f;

    [DataField, AutoNetworkedField]
    public float OozeDuration = 10f;

    public float Duration => SpurtDuration + OozeDuration;

    [DataField, AutoNetworkedField]
    public float SpurtSpeed = 16f;

    [DataField, AutoNetworkedField]
    public float SpurtSpread = 0.26f;

    [DataField, AutoNetworkedField]
    public float SpurtEmitRate = 90f;

    [DataField, AutoNetworkedField]
    public float OozeSpeed = 1.2f;

    [DataField, AutoNetworkedField]
    public float OozeEmitRate = 18f;

    [DataField, AutoNetworkedField]
    public float OozeReach = 1f;

    [DataField, AutoNetworkedField]
    public Vector2 OozeGravity = new(0f, -3.5f);

    [DataField, AutoNetworkedField]
    public float TransitionDuration = 0.35f;

    [DataField, AutoNetworkedField]
    public float MaxReach = 2f;

    [DataField, AutoNetworkedField]
    public Color Color = new(0.5f, 0.015f, 0.015f);
}

