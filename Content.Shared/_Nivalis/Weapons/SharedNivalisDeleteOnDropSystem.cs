using Content.Shared.Interaction.Events;
using Robust.Shared.Network;

namespace Content.Shared._Nivalis.Weapons;

public abstract partial class SharedNivalisDeleteOnDropSystem : EntitySystem
{
    [Dependency] protected INetManager Net = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<NivalisDeleteOnDropComponent, DroppedEvent>(OnDropped);
    }

    private void OnDropped(EntityUid uid, NivalisDeleteOnDropComponent component, DroppedEvent args)
    {
        if (!Net.IsServer)
            return;

        QueueDel(uid);
    }
}

