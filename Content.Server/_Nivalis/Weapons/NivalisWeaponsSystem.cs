using Content.Server.Interaction;
using Content.Shared._Nivalis.Weapons;
using Content.Shared.Actions;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Content.Shared.UserInterface;
using Robust.Server.GameObjects;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;

namespace Content.Server._Nivalis.Weapons;

/// <summary>
///     Server-side power for the Nivalis weapons &amp; ammo pool system.
/// </summary>
public sealed partial class NivalisWeaponsSystem : SharedNivalisWeaponsSystem
{
    [Dependency] private readonly SharedUserInterfaceSystem _ui = default!;
    [Dependency] private readonly SharedActionsSystem _actions = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<NivalisAmmoBoxComponent, ActivateInWorldEvent>(OnAmmoBoxActivate);
        SubscribeLocalEvent<NivalisAmmoHudComponent, MapInitEvent>(OnAmmoHudMapInit);
        SubscribeLocalEvent<NivalisAmmoHudComponent, NivalisOpenAmmoMenuEvent>(OnOpenAmmoMenu);
    }

    private void OnOpenAmmoMenu(Entity<NivalisAmmoHudComponent> ent, ref NivalisOpenAmmoMenuEvent args)
    {
        if (args.Performer != ent.Owner)
            return;

        if (!TryComp<ActorComponent>(ent.Owner, out var actor))
            return;

        OpenAmmoMenu(ent.Owner, actor.PlayerSession);
    }

    protected override void InitializeWeapons()
    {
        base.InitializeWeapons();
    }
    /// <summary>
    ///     Interacting with an ammo box (E key) adds its ammo to the player's pool.
    /// </summary>
    private void OnAmmoBoxActivate(Entity<NivalisAmmoBoxComponent> box, ref ActivateInWorldEvent args)
    {
        if (args.Handled || !args.Complex)
            return;

        var user = args.User;
        if (!TryComp<NivalisAmmoPoolComponent>(user, out var pool))
            return;

        var amount = box.Comp.Amount ?? pool.AmmoPickupAmount;
        var current = pool.GetAmmo(box.Comp.AmmoType);
        pool.SetAmmo(box.Comp.AmmoType, current + amount);

        Dirty(user, pool);

        var locName = GetAmmoTypeName(box.Comp.AmmoType);
        _popup.PopupEntity(Loc.GetString("nivalis-ammo-pickup",
            ("amount", amount), ("type", locName)), user, user, PopupType.Medium);

        QueueDel(box);
        args.Handled = true;
    }

    /// <summary>
    ///     Grants the ammo menu action when the HUD component is first placed.
    /// </summary>
    private void OnAmmoHudMapInit(Entity<NivalisAmmoHudComponent> ent, ref MapInitEvent args)
    {
        _actions.AddAction(ent, ent.Comp.OpenAmmoMenuAction);

        // Make sure the pool exists alongside the HUD.
        var pool = EnsureComp<NivalisAmmoPoolComponent>(ent);

        // Seed any configured starting ammo once.
        if (!pool.Seeded)
        {
            foreach (var (type, count) in pool.StartingAmmo)
            {
                pool.SetAmmo(type, count);
            }
            pool.Seeded = true;
            Dirty(ent, pool);
        }

        // Register the BUI so the client can open the ammo menu on this entity.
        var ui = EnsureComp<UserInterfaceComponent>(ent);
        if (!_ui.HasUi(ent, NivalisAmmoMenuUiKey.Key, ui))
            _ui.SetUi((ent, ui), NivalisAmmoMenuUiKey.Key, new InterfaceData("NivalisAmmoMenuBoundUserInterface"));
    }

    /// <summary>
    ///     Refreshes and opens the ammo menu for a player.
    /// </summary>
    public void OpenAmmoMenu(EntityUid owner, ICommonSession? session = null)
    {
        if (session == null)
        {
            if (!TryComp<ActorComponent>(owner, out var actor))
                return;
            session = actor.PlayerSession;
        }

        var pool = EnsureComp<NivalisAmmoPoolComponent>(owner);
        var state = new NivalisAmmoMenuUiState();

        foreach (NivalisAmmoType type in Enum.GetValues<NivalisAmmoType>())
        {
            state.Entries.Add(new NivalisAmmoMenuEntry
            {
                Type = type,
                Name = GetAmmoTypeName(type),
                IconPath = GetAmmoIcon(type),
                Count = pool.GetAmmo(type),
            });
        }

        _ui.SetUiState(owner, NivalisAmmoMenuUiKey.Key, state);
        _ui.OpenUi(owner, NivalisAmmoMenuUiKey.Key, session);
    }

    private string GetAmmoTypeName(NivalisAmmoType type)
    {
        return Loc.GetString($"nivalis-ammo-{type.ToString().ToLowerInvariant()}");
    }

    private string GetAmmoIcon(NivalisAmmoType type)
    {
        switch (type)
        {
            case NivalisAmmoType.Light:   return "/Textures/Objects/Weapons/Guns/Ammunition/Boxes/pistol.rsi/base.png";
            case NivalisAmmoType.Short:   return "/Textures/Objects/Weapons/Guns/Ammunition/Boxes/light_rifle.rsi/base.png";
            case NivalisAmmoType.Long:    return "/Textures/Objects/Weapons/Guns/Ammunition/Boxes/rifle.rsi/base.png";
            case NivalisAmmoType.Small:   return "/Textures/Objects/Weapons/Guns/Ammunition/Boxes/caseless_rifle.rsi/base.png";
            case NivalisAmmoType.Shell:   return "/Textures/Objects/Weapons/Guns/Ammunition/Boxes/shotgun.rsi/base.png";
            case NivalisAmmoType.Medium:  return "/Textures/Objects/Weapons/Guns/Ammunition/Boxes/magnum.rsi/base.png";
            case NivalisAmmoType.Heavy:   return "/Textures/Objects/Weapons/Guns/Ammunition/Boxes/anti_materiel.rsi/base.png";
            default:                      return "/Textures/Interface/Actions/ammo.png";
        }
    }
}

