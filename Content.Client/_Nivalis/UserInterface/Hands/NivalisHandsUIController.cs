using System.Collections.Generic;
using Content.Client.Gameplay;
using Content.Client.Hands.Systems;
using Content.Client.UserInterface.Systems.Gameplay;
using Content.Client._Nivalis.UserInterface.Lobby;
using Content.Shared.Hands.Components;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.Controllers;
using Robust.Shared.GameObjects;
using Robust.Shared.Timing;

namespace Content.Client._Nivalis.UserInterface.Hands;

public sealed partial class NivalisHandsUIController : UIController, IOnSystemChanged<HandsSystem>
{
    [Dependency] private IEntityManager _entities = default!;
    private HandsSystem? _handsSystem;

    public override void Initialize()
    {
        var gameplayStateLoad = UIManager.GetUIController<GameplayStateLoadController>();
        gameplayStateLoad.OnScreenLoad += OnScreenLoad;
        gameplayStateLoad.OnScreenUnload += OnScreenUnload;
    }

    private void OnScreenLoad()
    {
        if (_hands != null)
            Rebuild();
    }

    private void OnScreenUnload()
    {
        var bar = Bar;
        if (bar != null)
        {
            bar.Visible = false;
            bar.ClearHandButtons();
        }
    }

    private NivalisHandsBar? Bar => UIManager.GetActiveUIWidgetOrNull<NivalisHandsBar>();

    private Entity<HandsComponent>? _hands;

    public void OnSystemLoaded(HandsSystem system)
    {
        _handsSystem = system;
        _handsSystem.OnPlayerHandsAdded += OnHandsAdded;
        _handsSystem.OnPlayerHandsRemoved += OnHandsRemoved;
        _handsSystem.OnPlayerItemAdded += OnItemAdded;
        _handsSystem.OnPlayerItemRemoved += OnItemRemoved;
        _handsSystem.OnPlayerSetActiveHand += OnSetActiveHand;
    }

    public void OnSystemUnloaded(HandsSystem system)
    {
        _handsSystem!.OnPlayerHandsAdded -= OnHandsAdded;
        _handsSystem.OnPlayerHandsRemoved -= OnHandsRemoved;
        _handsSystem.OnPlayerItemAdded -= OnItemAdded;
        _handsSystem.OnPlayerItemRemoved -= OnItemRemoved;
        _handsSystem.OnPlayerSetActiveHand -= OnSetActiveHand;
        _handsSystem = null;
    }

    private void OnHandsAdded(Entity<HandsComponent> hands)
    {
        _hands = hands;
        Rebuild();
    }

    private void OnHandsRemoved()
    {
        _hands = null;
        var bar = Bar;
        if (bar == null)
            return;
        bar.ClearHandButtons();
        bar.Visible = false;
    }

    private void OnItemAdded(string handName, EntityUid entity)
    {
        Refresh();
    }

    private void OnItemRemoved(string handName, EntityUid entity)
    {
        Refresh();
    }

    private void OnSetActiveHand(string? handName)
    {
        var bar = Bar;
        if (bar == null || _hands is not { } hands)
            return;

        var activeIndex = 0;
        var index = 0;
        foreach (var hand in hands.Comp.SortedHands)
        {
            if (hand == handName)
            {
                activeIndex = index;
                break;
            }

            index++;
        }

        bar.SetActive(activeIndex);
    }

    private void Rebuild()
    {
        var bar = Bar;
        if (bar == null || _hands is not { } hands)
            return;

        bar.Visible = true;
        bar.ClearHandButtons();

        var names = new List<string?>();
        var activeIndex = 0;
        var index = 0;
        foreach (var handName in hands.Comp.SortedHands)
        {
            var isActive = handName == hands.Comp.ActiveHandId;
            if (isActive)
                activeIndex = index;
            var button = new SlantedButton();
            var captured = handName;
            button.OnPressed += _ =>
            {
                if (_handsSystem != null)
                    _handsSystem.UIHandClick(hands, captured);
            };

            bar.AddHandButton(button, index, isActive);
            names.Add(GetHeldItemName(hands, handName));
            index++;
        }

        bar.SetItems(names);
        bar.SetActive(activeIndex);
    }

    private void Refresh()
    {
        var bar = Bar;
        if (bar == null || _hands is not { } hands)
            return;

        bar.Visible = true;

        var names = new List<string?>();
        foreach (var handName in hands.Comp.SortedHands)
        {
            names.Add(GetHeldItemName(hands, handName));
        }

        bar.SetItems(names);
    }

    private string? GetHeldItemName(Entity<HandsComponent> hands, string? handName)
    {
        if (_handsSystem == null || handName == null)
            return null;

        if (_handsSystem.TryGetHeldItem((hands.Owner, hands.Comp), handName, out var held) &&
            held is { } entity && _entities.TryGetComponent<MetaDataComponent>(entity, out var meta))
        {
            return meta.EntityName;
            }

        return null;
            }

    public override void FrameUpdate(FrameEventArgs args)
    {
        base.FrameUpdate(args);
        if (Bar is { } bar)
            bar.UpdateFrame(args.DeltaSeconds);
    }
}
