using Robust.Shared.Timing;

namespace Content.Server._Nivalis.Weapons;

[RegisterComponent]
public sealed partial class NivalisDecimatorMarkComponent : Component
{
    [DataField]
    public TimeSpan ExpiresAt;
}
