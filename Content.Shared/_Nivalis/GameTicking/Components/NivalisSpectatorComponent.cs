using Robust.Shared.GameStates;
using Robust.Shared.Network;
using Robust.Shared.Serialization;

namespace Content.Shared._Nivalis.GameTicking.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class NivalisSpectatorComponent : Component
{
    [DataField]
    public NetUserId Player;

    [DataField, AutoNetworkedField]
    public bool IsAdmin;

    [DataField]
    public EntityUid FollowTarget;
}

[Serializable, NetSerializable]
public sealed class NivalisSpectateCycleMessage : EntityEventArgs
{
    public bool Next;
    public NivalisSpectateCycleMessage(bool next) => Next = next;
}
