using Content.Shared._Nivalis.Weapons;
using Robust.Client.UserInterface;

namespace Content.Client._Nivalis.UserInterface.AmmoMenu;

public sealed class NivalisAmmoMenuBoundUserInterface : BoundUserInterface
{
    private NivalisAmmoMenuWindow? _window;

    public NivalisAmmoMenuBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
    }

    protected override void Open()
    {
        base.Open();
        _window = this.CreateWindow<NivalisAmmoMenuWindow>();
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);

        if (state is not NivalisAmmoMenuUiState data)
            return;

        _window?.Populate(data.Entries);
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (_window is not null)
            _window.Dispose();
        _window = null;
    }
}
