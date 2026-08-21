using System.Collections.Generic;
using Robust.Shared.Serialization;

namespace Content.Shared._Nivalis.Weapons;

[Serializable, NetSerializable]
public enum NivalisAmmoMenuUiKey : byte
{
    Key,
}

/// <summary>
///     A single entry in the ammo menu, describing one ammo type and its current count.
/// </summary>
[Serializable, NetSerializable]
public sealed class NivalisAmmoMenuEntry
{
    public NivalisAmmoType Type;
    public string Name = default!;
    public string IconPath = default!;
    public int Count;
}

/// <summary>
///     State sent to the client's ammo menu GUI.
/// </summary>
[Serializable, NetSerializable]
public sealed class NivalisAmmoMenuUiState : BoundUserInterfaceState
{
    public List<NivalisAmmoMenuEntry> Entries = new();
}
