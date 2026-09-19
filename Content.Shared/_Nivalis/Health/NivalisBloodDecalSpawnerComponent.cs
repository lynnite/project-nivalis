using System.Numerics;
using Robust.Shared.GameStates;

namespace Content.Shared._Nivalis.Health;

[RegisterComponent, NetworkedComponent]
public sealed partial class NivalisBloodDecalSpawnerComponent : Component
{
    [DataField]
    public int Remaining = 5;

    [DataField]
    public float Interval = 0.1f;

    [DataField]
    public Vector2 Direction = new(-1f, 0f);

    [DataField]
    public int Width = 3;

    [DataField]
    public int Height = 1;

    [DataField]
    public TimeSpan NextSpawn;
}
