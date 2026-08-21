using System.Collections.Generic;
using Robust.Shared.GameStates;
using Robust.Shared.GameStates;

namespace Content.Shared._Nivalis.Weapons;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class NivalisAmmoPoolComponent : Component
{
    [DataField, AutoNetworkedField]
    public int AmmoPickupAmount = 12;

    [DataField, AutoNetworkedField]
    public Dictionary<NivalisAmmoType, int> Ammo = new();

    [DataField]
    public Dictionary<NivalisAmmoType, int> StartingAmmo = new()
    {
        [NivalisAmmoType.Light] = 48,
        [NivalisAmmoType.Short] = 60,
        [NivalisAmmoType.Long] = 40,
        [NivalisAmmoType.Small] = 48,
        [NivalisAmmoType.Shell] = 16,
        [NivalisAmmoType.Medium] = 40,
        [NivalisAmmoType.Heavy] = 12,
    };

    [DataField, AutoNetworkedField]
    public bool Seeded;
    public int GetAmmo(NivalisAmmoType type)
    {
        return Ammo.TryGetValue(type, out var count) ? count : 0;
    }

    public void SetAmmo(NivalisAmmoType type, int count)
    {
        if (count <= 0)
            Ammo.Remove(type);
        else
            Ammo[type] = count;
    }
}

