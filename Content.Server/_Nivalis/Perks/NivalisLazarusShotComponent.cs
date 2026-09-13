using System.Numerics;

namespace Content.Server._Nivalis.Perks;

[RegisterComponent, Access(typeof(NivalisLazarusSystem))]
public sealed partial class NivalisLazarusShotComponent : Component
{
    public EntityUid Shooter;

    public Vector2 Target;

    public bool Heal;

    public bool Detonated;
}
