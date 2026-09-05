using System;
using Content.Server.Popups;
using Content.Server._Nivalis.Scrap;
using Content.Shared.Interaction;
using Content.Shared.Mobs.Components;
using Content.Shared._Nivalis.Recycling;
using Content.Shared._Nivalis.Scrap;
using Content.Shared.Whitelist;
using Robust.Shared.Audio.Systems;
using Robust.Shared.GameObjects;

namespace Content.Server._Nivalis.Recycling;

/// <summary>
///     Server logic for the flat-rate <see cref="NivalisRecyclerComponent"/>. When a survivor
///     interacts by inserting a held item, the item is destroyed and a fixed scrap amount is
///     credited directly to that survivor's <see cref="NivalisScrapComponent"/> pool.
/// </summary>
public sealed partial class NivalisRecyclerSystem : EntitySystem
{
    [Dependency] private PopupSystem _popup = default!;
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private EntityWhitelistSystem _whitelist = default!;
    [Dependency] private NivalisScrapSystem _scrap = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<NivalisRecyclerComponent, InteractUsingEvent>(OnInteractUsing);
    }

    private void OnInteractUsing(Entity<NivalisRecyclerComponent> recycler, ref InteractUsingEvent args)
    {
        if (args.Handled)
            return;

        if (!CanRecycle(recycler, args.Used))
            return;

        TryRecycle(recycler, args.Used, args.User);
        args.Handled = true;
    }

    private bool CanRecycle(Entity<NivalisRecyclerComponent> recycler, EntityUid used)
    {
        if (Deleted(used) || used == recycler.Owner)
            return false;

        var comp = recycler.Comp;
        if (!comp.Enabled)
            return false;

        if (HasComp<NivalisRecyclerComponent>(used))
            return false;

        if (HasComp<MobStateComponent>(used))
            return false;

        if (_whitelist.IsWhitelistFailOrNull(comp.Whitelist, used))
            return false;

        return true;
    }

    private void TryRecycle(Entity<NivalisRecyclerComponent> recycler, EntityUid used, EntityUid user)
    {
        var reward = MathF.Max(0f, recycler.Comp.BaseScrapReward);

        if (reward > 0f)
        {
            _scrap.GrantScrap(user, reward);
            _popup.PopupEntity(Loc.GetString("nivalis-recycler-reward", ("scrap", reward)), user, user);
        }

        if (recycler.Comp.CrushSound != null)
            _audio.PlayPvs(recycler.Comp.CrushSound, recycler.Owner);

        QueueDel(used);
    }
}
