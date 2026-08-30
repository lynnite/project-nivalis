using Content.Shared._Nivalis.GameTicking.Components;
using Content.Shared.Interaction.Events;
using Content.Shared.ActionBlocker;
using Content.Shared.Movement.Events;

namespace Content.Shared._Nivalis.GameTicking;

public sealed partial class NivalisSpectatorSystem : EntitySystem
{
    [Dependency] private ActionBlockerSystem _blocker = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<NivalisSpectatorComponent, ComponentInit>(OnSpectatorInit);
        SubscribeLocalEvent<NivalisSpectatorComponent, UpdateCanMoveEvent>(OnBlockMovement);
        SubscribeLocalEvent<NivalisSpectatorComponent, ChangeDirectionAttemptEvent>(OnBlockMovement);
    }

    private void OnSpectatorInit(Entity<NivalisSpectatorComponent> ent, ref ComponentInit args)
    {
        _blocker.UpdateCanMove(ent);
    }

    private void OnBlockMovement(EntityUid uid, NivalisSpectatorComponent component, CancellableEntityEventArgs args)
    {
        args.Cancel();
    }
}

