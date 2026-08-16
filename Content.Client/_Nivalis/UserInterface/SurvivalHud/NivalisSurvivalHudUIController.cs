using Content.Client.Gameplay;
using Content.Client.UserInterface.Systems.Gameplay;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controllers;

namespace Content.Client._Nivalis.UserInterface.SurvivalHud;

public sealed partial class NivalisSurvivalHudUIController : UIController
{
    private NivalisSurvivalHud? Hud => UIManager.GetActiveUIWidgetOrNull<NivalisSurvivalHud>();

    public override void Initialize()
    {
        base.Initialize();

        var gameplayStateLoad = UIManager.GetUIController<GameplayStateLoadController>();
        gameplayStateLoad.OnScreenUnload += OnScreenUnload;
    }

    private void OnScreenUnload()
    {
        var hud = Hud;
        if (hud != null)
            hud.Visible = false;
    }
}
