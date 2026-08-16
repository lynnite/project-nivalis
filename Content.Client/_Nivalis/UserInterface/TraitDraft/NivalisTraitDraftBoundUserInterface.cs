using Content.Shared._Nivalis.Traits;
using Robust.Client.UserInterface;

namespace Content.Client._Nivalis.UserInterface.TraitDraft;

public sealed class NivalisTraitDraftBoundUserInterface : BoundUserInterface
{
    private NivalisTraitDraftWindow? _window;

    public NivalisTraitDraftBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
    }

    protected override void Open()
    {
        base.Open();
        _window = this.CreateWindow<NivalisTraitDraftWindow>();
        _window.OnTraitPicked += traitId =>
        {
            SendMessage(new NivalisTraitDraftSelectedMessage(traitId, Owner));
        };
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);

        if (state is not NivalisTraitDraftUiState data)
            return;

        _window?.Populate(data.Choices);
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (_window is not null)
            _window.Dispose();
        _window = null;
    }
}
