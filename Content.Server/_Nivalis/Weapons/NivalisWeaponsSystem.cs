using Content.Server.Interaction;
using Content.Server.NPC.Components;
using Content.Shared._Nivalis.Weapons;
using Content.Shared.Actions;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Interaction;
using Content.Shared.Movement.Components;
using Content.Shared.Popups;
using Content.Shared.UserInterface;
using Robust.Server.GameObjects;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;

namespace Content.Server._Nivalis.Weapons;

public sealed partial class NivalisWeaponsSystem : SharedNivalisWeaponsSystem
{
    [Dependency] private SharedUserInterfaceSystem _ui = default!;
    [Dependency] private SharedActionsSystem _actions = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private SharedHandsSystem _hands = default!;
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<NivalisAmmoBoxComponent, ActivateInWorldEvent>(OnAmmoBoxActivate);
        SubscribeLocalEvent<NivalisAmmoHudComponent, MapInitEvent>(OnAmmoHudMapInit);
        SubscribeLocalEvent<NivalisAmmoHudComponent, NivalisOpenAmmoMenuEvent>(OnOpenAmmoMenu);

        Subs.BuiEvents<NivalisAmmoHudComponent>(NivalisAmmoMenuUiKey.Key,
            subs => subs.Event<NivalisAmmoMenuDropAmmoMessage>(OnDropAmmoMessage));
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<NPCRangedCombatComponent, InputMoverComponent>();
        while (query.MoveNext(out var npc, out _, out _))
        {
            foreach (var held in _hands.EnumerateHeld(npc))
            {
                if (!TryComp<NivalisGunComponent>(held, out var gun) ||
                    gun.Reloading ||
                    gun.MagazineCount > 0)
                {
                    continue;
                }

                TryReload(held, npc);
                break;
            }
        }
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

    private void OnDropAmmoMessage(Entity<NivalisAmmoHudComponent> ent, ref NivalisAmmoMenuDropAmmoMessage args)
    {
        if (args.Actor != ent.Owner)
            return;

        if (args.Amount <= 0)
            return;

        if (!TryComp<NivalisAmmoPoolComponent>(ent.Owner, out var pool))
            return;

        var available = pool.GetAmmo(args.Type);
        if (available <= 0)
            return;

        var amount = Math.Min(available, args.Amount);
        if (amount <= 0)
            return;

        pool.SetAmmo(args.Type, available - amount);
        Dirty(ent.Owner, pool);

        var boxProto = GetAmmoBoxProto(args.Type);
        if (!string.IsNullOrEmpty(boxProto))
        {
            var box = Spawn(boxProto, Transform(ent.Owner).Coordinates);
            if (TryComp<NivalisAmmoBoxComponent>(box, out var boxComp))
            {
                boxComp.Amount = amount;
                Dirty(box, boxComp);
            }
        }

        var locName = GetAmmoTypeName(args.Type);
        _popup.PopupEntity(Loc.GetString("nivalis-ammo-drop",
            ("amount", amount), ("type", locName)), ent.Owner, ent.Owner, PopupType.Medium);

    }

    private void OnAmmoHudMapInit(Entity<NivalisAmmoHudComponent> ent, ref MapInitEvent args)
    {
        _actions.AddAction(ent, ent.Comp.OpenAmmoMenuAction);

        var pool = EnsureComp<NivalisAmmoPoolComponent>(ent);

        if (!pool.Seeded)
        {
            foreach (var (type, count) in pool.StartingAmmo)
            {
                pool.SetAmmo(type, count);
            }
            pool.Seeded = true;
            Dirty(ent, pool);
        }

        var ui = EnsureComp<UserInterfaceComponent>(ent);
        if (!_ui.HasUi(ent, NivalisAmmoMenuUiKey.Key, ui))
            _ui.SetUi((ent, ui), NivalisAmmoMenuUiKey.Key, new InterfaceData("NivalisAmmoMenuBoundUserInterface"));
    }

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

    private string GetAmmoBoxProto(NivalisAmmoType type)
    {
        switch (type)
        {
            case NivalisAmmoType.Light: return "NivalisAmmoBoxLight";
            case NivalisAmmoType.Short: return "NivalisAmmoBoxShort";
            case NivalisAmmoType.Long: return "NivalisAmmoBoxLong";
            case NivalisAmmoType.Small: return "NivalisAmmoBoxSmall";
            case NivalisAmmoType.Shell: return "NivalisAmmoBoxShell";
            case NivalisAmmoType.Medium: return "NivalisAmmoBoxMedium";
            case NivalisAmmoType.Heavy: return "NivalisAmmoBoxHeavy";
            default: return string.Empty;
        }
    }

    private string GetAmmoIcon(NivalisAmmoType type)
    {
        switch (type)
        {
            case NivalisAmmoType.Light: return "/Textures/Objects/Weapons/Guns/Ammunition/Boxes/pistol.rsi/base.png";
            case NivalisAmmoType.Short: return "/Textures/Objects/Weapons/Guns/Ammunition/Boxes/light_rifle.rsi/base.png";
            case NivalisAmmoType.Long: return "/Textures/Objects/Weapons/Guns/Ammunition/Boxes/rifle.rsi/base.png";
            case NivalisAmmoType.Small: return "/Textures/Objects/Weapons/Guns/Ammunition/Boxes/caseless_rifle.rsi/base.png";
            case NivalisAmmoType.Shell: return "/Textures/Objects/Weapons/Guns/Ammunition/Boxes/shotgun.rsi/base.png";
            case NivalisAmmoType.Medium: return "/Textures/Objects/Weapons/Guns/Ammunition/Boxes/magnum.rsi/base.png";
            case NivalisAmmoType.Heavy: return "/Textures/Objects/Weapons/Guns/Ammunition/Boxes/anti_materiel.rsi/base.png";
            default: return "/Textures/Interface/Actions/ammo.png";
        }
    }
}

