using System.Collections.Generic;

using Robust.Shared.Serialization;

namespace Content.Shared._Nivalis.Weapons;

[Serializable, NetSerializable]
public enum NivalisAmmoMenuUiKey : byte
{
    Key,
}

[Serializable, NetSerializable]
public sealed class NivalisAmmoMenuEntry
{
    public NivalisAmmoType Type;
    public string Name = default!;
    public string IconPath = default!;
    public int Count;
}

[Serializable, NetSerializable]
public sealed class NivalisAmmoMenuUiState : BoundUserInterfaceState
{
    public List<NivalisAmmoMenuEntry> Entries = new();
}

[Serializable, NetSerializable]
public sealed class NivalisAmmoMenuDropAmmoMessage : BoundUserInterfaceMessage
{
    public NivalisAmmoType Type;
    public int Amount;
}

