using System.Linq;
using Content.Shared.Hands.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Interaction.Events;
using Content.Shared.Popups;
using Robust.Shared.Audio.Systems;

namespace Content.Shared._Nivalis.Hands;

public abstract partial class SharedNivalisHandsSystem : EntitySystem
{
    [Dependency] private SharedHandsSystem _hands = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private SharedAudioSystem _audio = default!;

    public int EnsureHandCount(Entity<HandsComponent?> user, int count)
    {
        if (!Resolve(user, ref user.Comp, false))
            return 0;

        var current = _hands.EnumerateHands(user).Count();
        var added = 0;

        while (current + added < count)
        {
            var id = $"nivalis_hand_{added + 1}";
            _hands.AddHand(user, id, HandLocation.Middle);
            added++;
        }

        return added;
    }

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<NivalisExtraHandsComponent, UseInHandEvent>(OnUseInHand);
    }

    private void OnUseInHand(Entity<NivalisExtraHandsComponent> ent, ref UseInHandEvent args)
    {
        var user = args.User;

        if (!TryComp(user, out HandsComponent? handsComp))
            return;

        if (ent.Comp.Used || ent.Comp.MaxTotalHands <= 0)
        {
            _popup.PopupEntity(Loc.GetString("nivalis-hands-already-used"), user, user);
            return;
        }

        var current = _hands.EnumerateHands(user).Count();
        if (current >= ent.Comp.MaxTotalHands)
        {
            _popup.PopupEntity(Loc.GetString("nivalis-hands-already-used"), user, user);
            return;
        }

        var toAdd = Math.Min(ent.Comp.Hands, ent.Comp.MaxTotalHands - current);
        if (toAdd <= 0)
            return;

        var added = 0;
        for (var i = 0; i < toAdd; i++)
        {
            var id = $"backpack_hand_{ent.Owner.Id}_{i}";
            if (_hands.TryGetHand((user, handsComp), id, out _))
                continue;
            _hands.AddHand((user, handsComp), id, ent.Comp.Location);
            added++;
        }

        if (added == 0)
            return;

        ent.Comp.Used = true;
        Dirty(ent);

        _popup.PopupEntity(Loc.GetString("nivalis-hands-gained", ("count", added)), user, user);
        _audio.PlayPvs("/Audio/Effects/pop.ogg", user);
    }
}

