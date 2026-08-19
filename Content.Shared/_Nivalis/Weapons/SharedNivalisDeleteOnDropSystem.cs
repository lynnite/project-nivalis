using Content.Shared.Interaction.Events;

namespace Content.Shared._Nivalis.Weapons;

public abstract partial class SharedNivalisDeleteOnDropSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<NivalisDeleteOnDropComponent, DroppedEvent>(OnDropped);
    }

    private void OnDropped(EntityUid uid, NivalisDeleteOnDropComponent component, DroppedEvent args)
    {
        QueueDel(uid);
    }
}
