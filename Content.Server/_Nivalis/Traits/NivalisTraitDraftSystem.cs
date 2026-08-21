using Content.Server._Nivalis.Perks;
using Content.Shared._Nivalis.Perks;
using Content.Shared._Nivalis.Traits;
using Content.Shared.UserInterface;
using Robust.Server.Player;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server._Nivalis.Traits;

public sealed partial class NivalisTraitDraftSystem : EntitySystem
{
    [Dependency] private NivalisPerkSystem _perks = default!;
    [Dependency] private SharedUserInterfaceSystem _ui = default!;
    [Dependency] private IPrototypeManager _proto = default!;
    [Dependency] private IRobustRandom _random = default!;

    public override void Initialize()
    {
        base.Initialize();

        Subs.BuiEvents<NivalisTraitDraftComponent>(NivalisTraitDraftUiKey.Key,
            subs =>
            {
                subs.Event<NivalisTraitDraftSelectedMessage>(OnTraitSelected);
            });
    }

    public void OpenTraitDraft(EntityUid owner, NivalisPerkComponent perk, int count)
    {
        var candidates = _perks.GetDraftChoices((owner, perk), count, _random);
        if (candidates.Count == 0)
            return;

        if (!TryComp<ActorComponent>(owner, out var actor))
            return;

        EnsureComp<NivalisTraitDraftComponent>(owner);
        var state = new NivalisTraitDraftUiState();
        foreach (var id in candidates)
        {
            if (!_proto.TryIndex(id, out var perkProto))
                continue;
            state.Choices.Add(new NivalisTraitDraftChoice
            {
                Id = id,
                Name = Loc.GetString(perkProto.Name),
                Description = Loc.GetString(perkProto.Description),
            });
        }

        if (state.Choices.Count == 0)
            return;

        if (_ui.HasUi(owner, NivalisTraitDraftUiKey.Key))
            _ui.SetUiState(owner, NivalisTraitDraftUiKey.Key, state);
        _ui.OpenUi(owner, NivalisTraitDraftUiKey.Key, actor.PlayerSession);
    }

    private void OnTraitSelected(Entity<NivalisTraitDraftComponent> ent, ref NivalisTraitDraftSelectedMessage args)
    {
        if (args.Actor != ent.Owner)
            return;

        var survivor = (Entity<NivalisPerkComponent?>)ent.Owner;
        _perks.AddPerk(survivor, args.TraitId);
        _ui.CloseUi(ent.Owner, NivalisTraitDraftUiKey.Key, (ICommonSession?)null);
        RemComp<NivalisTraitDraftComponent>(ent.Owner);
    }
}

