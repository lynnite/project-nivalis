using Content.Shared.Damage;
using Content.Shared.Damage.Systems;
using Robust.Shared.Containers;

namespace Content.Shared._Nivalis.Combat;

public sealed partial class SharedNivalisFriendlyFireSystem : EntitySystem
{
    [Dependency] private readonly SharedContainerSystem _container = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<NivalisFriendlyFireComponent, DamageModifyEvent>(OnDamageModify);
    }

    private void OnDamageModify(Entity<NivalisFriendlyFireComponent> victim, ref DamageModifyEvent args)
    {
        if (args.Origin is not { } origin || origin == victim.Owner)
            return;

        if (!TryGetAttackerTeam(origin, out var attackerTeam))
            return;

        if (attackerTeam == NivalisCombatTeam.None || attackerTeam != victim.Comp.Team)
            return;

        args.Damage = new DamageSpecifier();
    }

    private bool TryGetAttackerTeam(EntityUid origin, out NivalisCombatTeam team)
    {
        if (TryComp<NivalisFriendlyFireComponent>(origin, out var comp))
        {
            team = comp.Team;
            return true;
        }

        if (_container.TryGetContainingContainer((origin, null, null), out var container) &&
            TryComp<NivalisFriendlyFireComponent>(container.Owner, out var holderComp))
        {
            team = holderComp.Team;
            return true;
        }

        team = NivalisCombatTeam.None;
        return false;
    }
}
